using System.Collections.Concurrent;
using System.Threading.Channels;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.Services;

/// <summary>
/// The optional web stage, run as its own durable job after (or independently of) the
/// main ASIC/ABR enrichment. For each suitable record whose business name renews within
/// the lead window it reverse-whois-es the holder's ABN to find domains, then looks each
/// domain up on auda until an email is found, then (when a contact enricher is wired up)
/// pulls phone/socials. Mirrors <c>SearchProcessingService</c>: parallel fetch workers
/// with no DbContext access stream results to a single consumer that applies them and
/// saves in batches, so a redeploy mid-run resumes from the records still outstanding.
/// </summary>
public sealed class WebEnrichmentService(
    IApplicationDbContext db,
    IReverseWhoisClient reverseWhois,
    IAudaClient auda,
    IContactEnricher contact,
    IJobScheduler jobs,
    IOptionsMonitor<WebEnrichmentOptions> options,
    ILogger<WebEnrichmentService> logger) : IWebEnrichmentService
{
    private const int SaveBatchSize = 10;

    private enum Outcome { Enriched, NoWebsite, NoEmail, Failed }

    private sealed record FetchResult(
        BusinessNameRecord Record, Outcome Outcome, IReadOnlyList<string> Domains,
        string? PrimaryEmail, IReadOnlyList<string> Emails, ContactEnrichmentResult Contact, string? Error);

    public async Task ProcessAsync(Guid searchRunId, CancellationToken cancellationToken)
    {
        var run = await db.SearchRuns.FirstOrDefaultAsync(r => r.Id == searchRunId, cancellationToken);
        if (run is null)
        {
            logger.LogWarning("Web enrichment: search run {RunId} not found.", searchRunId);
            return;
        }

        if (!run.EnableWebEnrichment)
        {
            logger.LogInformation("Web enrichment not enabled for run {RunId}; nothing to do.", searchRunId);
            return;
        }

        if (!reverseWhois.IsConfigured)
        {
            logger.LogWarning("Web enrichment: no WhoisXML API key configured; skipping run {RunId}.", searchRunId);
            return;
        }

        var opts = options.CurrentValue;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            // Candidates: suitable, ASIC-enriched, with an ABN to search, not already in a
            // terminal web state (Enriched/NoWebsite/NoEmail). NotAttempted/Pending/Failed
            // are (re)processed — which is also how a resumed job picks up the remainder.
            var candidates = await db.BusinessNameRecords
                .Where(r => r.SearchRunId == searchRunId
                    && r.IsSuitable
                    && r.EnrichmentStatus == EnrichmentStatus.Enriched
                    && r.HolderAbn != null
                    && r.WebEnrichmentStatus != WebEnrichmentStatus.Enriched
                    && r.WebEnrichmentStatus != WebEnrichmentStatus.NoWebsite
                    && r.WebEnrichmentStatus != WebEnrichmentStatus.NoEmail)
                .ToListAsync(cancellationToken);

            // Only chase names renewing within the window; mark the rest Skipped so the UI
            // distinguishes "out of window" from "not reached".
            var eligible = new List<BusinessNameRecord>();
            foreach (var record in candidates)
            {
                if (RenewalWindow.IsWithinMonths(record.RenewalDate, today, opts.RenewalWindowMonths))
                {
                    record.WebEnrichmentStatus = WebEnrichmentStatus.Pending;
                    eligible.Add(record);
                }
                else
                {
                    record.WebEnrichmentStatus = WebEnrichmentStatus.Skipped;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Web enrichment for run {RunId}: {Eligible} eligible of {Candidates} suitable records.",
                searchRunId, eligible.Count, candidates.Count);

            await EnrichAllAsync(run, eligible, opts, cancellationToken);

            // Now that contact emails are populated, push to Ontraport if the run's config
            // opts in — deferred here (rather than at ASIC-completion) so the email rides along.
            var config = await db.OntraportConfigurations.FirstOrDefaultAsync(cancellationToken);
            if (config?.AutoPushEnabled == true)
            {
                jobs.EnqueuePush(run.Id);
            }
        }
        // Shutdown (deploy/restart): let Hangfire re-queue; the resume filter above picks
        // up the records still outstanding.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Web enrichment for run {RunId} interrupted by shutdown; will resume.", searchRunId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Web enrichment for run {RunId} failed.", searchRunId);
        }
    }

    private async Task EnrichAllAsync(
        SearchRun run, IReadOnlyList<BusinessNameRecord> work, WebEnrichmentOptions opts, CancellationToken cancellationToken)
    {
        if (work.Count == 0) return;

        var queue = new ConcurrentQueue<BusinessNameRecord>(work);
        var results = Channel.CreateUnbounded<FetchResult>(new UnboundedChannelOptions { SingleReader = true });

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stopToken = stop.Token;

        var concurrency = Math.Min(
            Math.Clamp(opts.MaxConcurrency, 1, WebEnrichmentOptions.MaxConcurrencyLimit), work.Count);

        var workers = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(async () =>
            {
                while (!stopToken.IsCancellationRequested && queue.TryDequeue(out var record))
                {
                    results.Writer.TryWrite(await FetchAsync(record, stopToken));
                }
            }, CancellationToken.None))
            .ToArray();

        _ = Task.WhenAll(workers).ContinueWith(
            t => results.Writer.TryComplete(
                t.Exception?.GetBaseException() is { } ex && ex is not OperationCanceledException ? ex : null),
            TaskScheduler.Default);

        var applied = 0;
        var consecutiveFailures = 0;
        var failureLimit = Math.Max(1, opts.ConsecutiveFailureLimit);

        try
        {
            await foreach (var fetch in results.Reader.ReadAllAsync(cancellationToken))
            {
                Apply(fetch);

                if (fetch.Outcome == Outcome.Failed)
                {
                    if (++consecutiveFailures >= failureLimit)
                    {
                        throw new InvalidOperationException(
                            $"Web enrichment stopped after {consecutiveFailures} consecutive failures: {fetch.Error}");
                    }
                }
                else
                {
                    consecutiveFailures = 0;
                }

                if (++applied % SaveBatchSize == 0)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    if (await IsCancelledAsync(run.Id, cancellationToken))
                    {
                        stop.Cancel();
                        break;
                    }
                }
            }
        }
        catch
        {
            stop.Cancel();
            await WaitForWorkersAsync(workers);
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }

        await WaitForWorkersAsync(workers);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Network half of one record — no DbContext access, safe on a worker thread.</summary>
    private async Task<FetchResult> FetchAsync(BusinessNameRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var domains = await reverseWhois.FindDomainsAsync(record.HolderAbn!, cancellationToken);
            if (domains.Count == 0)
            {
                return new FetchResult(record, Outcome.NoWebsite, [], null, [], ContactEnrichmentResult.Empty, null);
            }

            string? primaryEmail = null;
            IReadOnlyList<string> emails = [];
            string? lastLookupError = null;

            // Try each domain in turn; stop at the first that yields an email.
            foreach (var domain in domains)
            {
                var lookup = await auda.LookupAsync(domain, cancellationToken);
                if (!lookup.Succeeded)
                {
                    lastLookupError = lookup.Error ?? "auda lookup failed.";
                    continue;
                }

                if (lookup.Emails.Count > 0)
                {
                    primaryEmail = lookup.Emails[0];
                    emails = lookup.Emails;
                    break;
                }
            }

            // Contact step (phone/socials). No-op unless a real enricher is wired up.
            var contactResult = ContactEnrichmentResult.Empty;
            if (contact.IsConfigured)
            {
                contactResult = await contact.EnrichAsync(
                    new ContactEnrichmentInput(
                        record.BusinessName, record.HolderName,
                        record.AddressForServiceDocuments ?? record.PrincipalPlaceOfBusiness,
                        domains, primaryEmail),
                    cancellationToken);
            }

            var outcome = primaryEmail is not null
                ? Outcome.Enriched
                : lastLookupError is not null ? Outcome.Failed : Outcome.NoEmail;

            var error = outcome == Outcome.Failed
                ? $"All auda lookups failed for this record's domains. Last error: {lastLookupError}"
                : null;

            return new FetchResult(record, outcome, domains, primaryEmail, emails, contactResult, error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new FetchResult(record, Outcome.Failed, [], null, [], ContactEnrichmentResult.Empty, ex.Message);
        }
    }

    /// <summary>Applies one fetched result to its record. Runs on the DbContext's thread.</summary>
    private void Apply(FetchResult fetch)
    {
        var record = fetch.Record;

        record.Websites = fetch.Domains.Count > 0 ? string.Join(",", fetch.Domains) : null;
        record.ContactEmail = fetch.PrimaryEmail;
        record.ContactEmails = fetch.Emails.Count > 0 ? string.Join(";", fetch.Emails) : null;
        record.ContactPhone = fetch.Contact.Phone ?? record.ContactPhone;
        record.ContactSocials = fetch.Contact.Socials ?? record.ContactSocials;
        if (fetch.Contact.ExtraEmail is { Length: > 0 } extra && record.ContactEmail is null)
        {
            record.ContactEmail = extra;
        }

        record.WebEnrichmentStatus = fetch.Outcome switch
        {
            Outcome.Enriched => WebEnrichmentStatus.Enriched,
            Outcome.NoWebsite => WebEnrichmentStatus.NoWebsite,
            Outcome.NoEmail => WebEnrichmentStatus.NoEmail,
            _ => WebEnrichmentStatus.Failed
        };
        record.WebEnrichmentError = fetch.Error;

        if (fetch.Outcome == Outcome.Failed)
        {
            logger.LogWarning("Web enrichment failed for ABN {Abn}: {Error}", record.HolderAbn, fetch.Error);
        }
    }

    private static async Task WaitForWorkersAsync(Task[] workers)
    {
        try { await Task.WhenAll(workers); }
        catch { }
    }

    private async Task<bool> IsCancelledAsync(Guid runId, CancellationToken cancellationToken) =>
        await db.SearchRuns.AsNoTracking()
            .Where(r => r.Id == runId).Select(r => r.CancellationRequested)
            .FirstOrDefaultAsync(cancellationToken);
}
