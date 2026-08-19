using System.Collections.Concurrent;
using System.Threading.Channels;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Domain.Services;
using Businesstron.Infrastructure.ExternalClients.Asic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.Services;

/// <summary>
/// Ports the console <c>Worker</c> pipeline to a durable, scoped job — and streams
/// progress: the harvested source items are persisted immediately (visible in the
/// live monitor as "Pending"), then rows are enriched from ASIC + ABR by a pool of
/// parallel workers, each owning an isolated ASIC session. Results stream back to a
/// single consumer that applies them to the DbContext (not thread-safe) in batches.
/// </summary>
public sealed class SearchProcessingService(
    IApplicationDbContext db,
    IAsicRegistryClientFactory asicFactory,
    IAbrClient abr,
    IDataGovClient dataGov,
    IOntraportClient ontraport,
    IJobScheduler jobs,
    IOptionsMonitor<AsicOptions> asicOptions,
    ILogger<SearchProcessingService> logger) : ISearchProcessingService
{
    /// <summary>Enriched records per SaveChanges round trip (and cancellation check).</summary>
    private const int SaveBatchSize = 20;

    private sealed record WorkItem(string Abn, string? Name, BusinessNameRecord Placeholder, bool CountAsProcessed = true);

    /// <summary>The HTTP half of one item's enrichment, produced by a parallel worker.</summary>
    private sealed record FetchResult(WorkItem Item, AsicSearchResult? Asic, AbrDetails? Abr, Exception? Error);

    public async Task ProcessAsync(Guid searchRunId, CancellationToken cancellationToken)
    {
        var run = await db.SearchRuns.FirstOrDefaultAsync(r => r.Id == searchRunId, cancellationToken);
        if (run is null)
        {
            logger.LogWarning("Search run {RunId} not found.", searchRunId);
            return;
        }

        run.Status = SearchRunStatus.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        run.Error = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (await IsCancelledAsync(searchRunId, cancellationToken))
            {
                await StopAsCancelledAsync(run, cancellationToken);
                return;
            }

            var keywords = await ResolveKeywordsAsync(run, cancellationToken);
            var byName = run.Source == SearchSource.DataGov;

            // 1) Harvest the source items and persist them straight away so the monitor
            //    shows what was found in real time, before the slow ASIC enrichment.
            //    Idempotency guard: if this run already has records, a previous run of
            //    this job already harvested them (it was interrupted by a restart/redeploy
            //    and Hangfire re-queued it). Harvesting again would duplicate the whole
            //    list, so resume by enriching what's still pending instead.
            List<WorkItem> work;
            if (await db.BusinessNameRecords.AnyAsync(r => r.SearchRunId == run.Id, cancellationToken))
            {
                var pending = await db.BusinessNameRecords
                    .Where(r => r.SearchRunId == run.Id && r.EnrichmentStatus == EnrichmentStatus.Pending)
                    .ToListAsync(cancellationToken);

                work = pending
                    .Where(r => !string.IsNullOrWhiteSpace(r.HolderAbn))
                    .Select(r => new WorkItem(r.HolderAbn!, byName ? r.BusinessName : null, r))
                    .ToList();
            }
            else
            {
                work = run.Source == SearchSource.DataGov
                    ? await HarvestDataGovAsync(run, cancellationToken)
                    : HarvestAbnList(run);

                run.TotalItems = work.Count;
                run.FoundRecords = work.Count;
                await db.SaveChangesAsync(cancellationToken);
            }

            // 2) Enrich the harvested rows in place (expanding to extra rows per holder).
            if (!await EnrichAllAsync(run, work, byName, keywords, cancellationToken))
            {
                await StopAsCancelledAsync(run, cancellationToken);
                return;
            }

            run.Status = SearchRunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            await RecomputeCountersAsync(run, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await ContinueAfterEnrichmentAsync(run, cancellationToken);
        }
        // The server is shutting down (deploy/restart) — rethrow so Hangfire re-queues
        // the job instead of marking the run Failed. On restart the idempotency guard
        // above resumes enrichment of the records that are still Pending.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Search run {RunId} interrupted by shutdown; it will resume after restart.", searchRunId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search run {RunId} failed.", searchRunId);
            run.Status = SearchRunStatus.Failed;
            run.Error = ex.Message;
            run.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<WorkItem>> HarvestDataGovAsync(SearchRun run, CancellationToken cancellationToken)
    {
        var records = await dataGov.GetNewBusinessNamesAsync(
            run.StartDate!.Value, run.EndDate!.Value, cancellationToken);

        var work = new List<WorkItem>(records.Count);
        foreach (var record in records)
        {
            var placeholder = new BusinessNameRecord
            {
                SearchRunId = run.Id,
                BusinessName = record.Name,
                HolderAbn = record.Abn,
                EnrichmentStatus = EnrichmentStatus.Pending,
                OntraportStatus = OntraportPushStatus.NotPushed
            };
            db.BusinessNameRecords.Add(placeholder);
            work.Add(new WorkItem(record.Abn, record.Name, placeholder));
        }

        return work;
    }

    private List<WorkItem> HarvestAbnList(SearchRun run)
    {
        var abns = ParseAbns(run.AbnListRaw);
        var work = new List<WorkItem>(abns.Count);
        foreach (var abn in abns)
        {
            var placeholder = new BusinessNameRecord
            {
                SearchRunId = run.Id,
                HolderAbn = abn,
                EnrichmentStatus = EnrichmentStatus.Pending,
                OntraportStatus = OntraportPushStatus.NotPushed
            };
            db.BusinessNameRecords.Add(placeholder);
            work.Add(new WorkItem(abn, null, placeholder));
        }

        return work;
    }

    /// <summary>
    /// Enriches the work items with bounded parallelism. A pool of workers — each with
    /// its own ASIC client, because a session's cookies and ADF window state are
    /// per-instance — fetches ASIC + ABR data concurrently and streams results back;
    /// this method applies them to the change tracker single-threaded and saves in
    /// batches. Returns false when the run was cancelled by the user.
    /// </summary>
    private async Task<bool> EnrichAllAsync(
        SearchRun run, IReadOnlyList<WorkItem> work, bool byName,
        IReadOnlyList<string> keywords, CancellationToken cancellationToken)
    {
        if (work.Count == 0) return true;

        var abrCache = new ConcurrentDictionary<string, Lazy<Task<AbrDetails>>>(StringComparer.OrdinalIgnoreCase);
        var queue = new ConcurrentQueue<WorkItem>(work);
        var results = Channel.CreateUnbounded<FetchResult>(new UnboundedChannelOptions { SingleReader = true });

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stopToken = stop.Token;

        // IOptionsMonitor so a Settings-page edit applies to the next run without a
        // restart; the hard limit caps it regardless of what the config file says.
        var concurrency = Math.Min(
            Math.Clamp(asicOptions.CurrentValue.MaxConcurrency, 1, AsicSettings.MaxConcurrencyLimit),
            work.Count);

        var workers = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(async () =>
            {
                var client = asicFactory.Create();
                try
                {
                    while (!stopToken.IsCancellationRequested && queue.TryDequeue(out var item))
                    {
                        results.Writer.TryWrite(await FetchAsync(client, item, byName, abrCache, stopToken));
                    }
                }
                finally
                {
                    (client as IDisposable)?.Dispose();
                }
            }, CancellationToken.None))
            .ToArray();

        // Close the channel once every worker has finished so the read loop below ends;
        // a non-cancellation worker fault surfaces out of ReadAllAsync.
        _ = Task.WhenAll(workers).ContinueWith(
            t => results.Writer.TryComplete(
                t.Exception?.GetBaseException() is { } ex && ex is not OperationCanceledException ? ex : null),
            TaskScheduler.Default);

        var cancelled = false;
        var applied = 0;

        // Circuit breaker: if ASIC stops answering, every remaining item fails the same
        // way, so stop rather than walking the rest of the list against a dead endpoint.
        var failureLimit = Math.Max(1, asicOptions.CurrentValue.ConsecutiveFailureLimit);
        var consecutiveFailures = 0;

        try
        {
            await foreach (var fetch in results.Reader.ReadAllAsync(cancellationToken))
            {
                Apply(run, fetch, keywords);

                // Clamped: a retry can re-queue more rows than the original source-item
                // count, and a progress bar reading over 100% is worse than one that
                // saturates.
                if (fetch.Item.CountAsProcessed)
                {
                    run.ProcessedItems = Math.Min(run.ProcessedItems + 1, run.TotalItems);
                }

                // Only transport failures count. A legitimate "no match on ASIC" is a
                // normal outcome and must not trip the breaker on a healthy run.
                if (fetch.Error is not null)
                {
                    if (++consecutiveFailures >= failureLimit)
                    {
                        throw new AsicUnavailableException(consecutiveFailures, fetch.Error);
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
                        cancelled = true;
                        stop.Cancel();
                        break;
                    }
                }
            }
        }
        catch
        {
            // Stop the workers before rethrowing (and before `stop` is disposed).
            stop.Cancel();
            await WaitForWorkersAsync(workers);
            throw;
        }

        await WaitForWorkersAsync(workers);
        await db.SaveChangesAsync(cancellationToken);
        return !cancelled;
    }

    private static async Task WaitForWorkersAsync(Task[] workers)
    {
        // Worker failures already surface through the results channel; a cancelled
        // worker is the expected outcome of a stop signal. Nothing to rethrow here.
        try { await Task.WhenAll(workers); }
        catch { }
    }

    /// <summary>The HTTP half of one item's enrichment — no DbContext access, safe on a worker.</summary>
    private async Task<FetchResult> FetchAsync(
        IAsicRegistryClient asic, WorkItem item, bool byName,
        ConcurrentDictionary<string, Lazy<Task<AbrDetails>>> abrCache, CancellationToken cancellationToken)
    {
        try
        {
            var result = byName
                ? await WithRetryAsync(() => asic.SearchByAbnAndNameAsync(item.Abn, item.Name!, cancellationToken), cancellationToken)
                : await WithRetryAsync(() => asic.SearchByAbnAsync(item.Abn, cancellationToken), cancellationToken);

            AbrDetails? abrDetails = null;
            if (result.Succeeded)
            {
                // Lazy-per-ABN so workers racing on the same ABN share one ABR request.
                var lazy = abrCache.GetOrAdd(item.Abn,
                    abn => new Lazy<Task<AbrDetails>>(() => abr.GetAbnDetailsAsync(abn, CancellationToken.None)));
                try
                {
                    abrDetails = await lazy.Value.WaitAsync(cancellationToken);
                }
                catch
                {
                    // Don't let one transient ABR failure poison every duplicate of this ABN.
                    abrCache.TryRemove(new KeyValuePair<string, Lazy<Task<AbrDetails>>>(item.Abn, lazy));
                    throw;
                }
            }

            return new FetchResult(item, result, abrDetails, null);
        }
        // Only a genuine user/shutdown cancellation (the run's token is cancelled) should
        // abort the worker. An HttpClient.Timeout also surfaces as a TaskCanceledException,
        // but with the token NOT cancelled — that's just this record failing, so return it
        // as a failure and keep processing the rest of the list.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new FetchResult(item, null, null, ex);
        }
    }

    /// <summary>Applies one fetched result to the run's records. Must run on the DbContext's thread.</summary>
    private void Apply(SearchRun run, FetchResult fetch, IReadOnlyList<string> keywords)
    {
        var placeholder = fetch.Item.Placeholder;

        if (fetch.Error is not null)
        {
            placeholder.EnrichmentStatus = EnrichmentStatus.Failed;
            placeholder.EnrichmentError = fetch.Error.Message;
            run.ErrorCount++;
            logger.LogWarning(fetch.Error, "Failed to enrich ABN {Abn}.", fetch.Item.Abn);
            return;
        }

        var result = fetch.Asic!;
        if (!result.Succeeded)
        {
            placeholder.EnrichmentStatus = EnrichmentStatus.Failed;
            placeholder.EnrichmentError = result.Reason ?? "ASIC lookup failed.";
            run.ErrorCount++;
            return;
        }

        var abrDetails = fetch.Abr!;

        // Flatten (business name × holder), keeping the name even when a holder is absent.
        var pairs = result.BusinessNames
            .SelectMany(bn => bn.Holders.Count > 0
                ? bn.Holders.Select(h => (bn, holder: (AsicHolder?)h))
                : [(bn, holder: (AsicHolder?)null)])
            .ToList();

        if (pairs.Count == 0)
        {
            // No ASIC detail; keep the harvested name/ABN as a bare lead.
            ApplySuitability(placeholder, placeholder.BusinessName, keywords);
            placeholder.AbnStatus = abrDetails.AbnStatus;
            placeholder.Gst = abrDetails.Gst;
            placeholder.EnrichmentStatus = EnrichmentStatus.Enriched;
            placeholder.EnrichmentError = null;
            if (placeholder.IsSuitable) run.SuitableCount++;
            return;
        }

        for (var i = 0; i < pairs.Count; i++)
        {
            var (bn, holder) = pairs[i];
            var record = i == 0 ? placeholder : new BusinessNameRecord { SearchRunId = run.Id };

            Fill(record, bn, holder, abrDetails, result.HasMultiple);
            ApplySuitability(record, bn.Name, keywords);
            record.EnrichmentStatus = EnrichmentStatus.Enriched;
            record.EnrichmentError = null;

            if (i > 0)
            {
                db.BusinessNameRecords.Add(record);
                run.FoundRecords++;
            }

            if (record.IsSuitable) run.SuitableCount++;
        }
    }

    private static void Fill(BusinessNameRecord record, AsicBusinessName bn, AsicHolder? holder, AbrDetails abr, bool hasMultiple)
    {
        record.BusinessName = bn.Name ?? record.BusinessName;
        record.Status = bn.Status;
        record.RegistrationDate = bn.RegistrationDate;
        record.RenewalDate = bn.RenewalDate;
        record.CancelledDate = bn.CancelledDate;
        record.CancellationUnderReview = bn.CancellationUnderReview;
        record.AddressForServiceDocuments = bn.AddressForServiceDocuments;
        record.PrincipalPlaceOfBusiness = bn.PrincipalPlaceOfBusiness;
        record.HolderName = holder?.Name;
        record.HolderType = holder?.Type;
        record.HolderAbn = holder?.Abn ?? record.HolderAbn;
        record.AbnStatus = abr.AbnStatus;
        record.Gst = abr.Gst;
        record.HasMultipleBusinessNames = hasMultiple;
    }

    private static void ApplySuitability(BusinessNameRecord record, string? name, IReadOnlyList<string> keywords)
    {
        var suitability = SuitabilityEvaluator.Evaluate(name, keywords);
        record.IsSuitable = suitability.IsSuitable;
        record.SuitabilityReason = suitability.MatchedKeyword;
        if (!suitability.IsSuitable)
        {
            record.OntraportStatus = OntraportPushStatus.Skipped;
        }
    }

    public async Task PushToOntraportAsync(Guid searchRunId, CancellationToken cancellationToken)
    {
        var run = await db.SearchRuns.FirstOrDefaultAsync(r => r.Id == searchRunId, cancellationToken);
        if (run is null) return;

        var config = await db.OntraportConfigurations.FirstOrDefaultAsync(cancellationToken);
        await PushInternalAsync(run, config?.TagId, config?.SequenceId, cancellationToken);
    }

    public async Task RetryFailedAsync(Guid searchRunId, CancellationToken cancellationToken)
    {
        var run = await db.SearchRuns.FirstOrDefaultAsync(r => r.Id == searchRunId, cancellationToken);
        if (run is null) return;

        run.Status = SearchRunStatus.Running;
        run.CancellationRequested = false;
        run.CompletedAt = null;
        run.Error = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var keywords = await ResolveKeywordsAsync(run, cancellationToken);
            var byName = run.Source == SearchSource.DataGov;

            var toRetry = await db.BusinessNameRecords
                .Where(r => r.SearchRunId == searchRunId
                    && (r.EnrichmentStatus == EnrichmentStatus.Failed || r.EnrichmentStatus == EnrichmentStatus.Pending))
                .ToListAsync(cancellationToken);

            var work = new List<WorkItem>(toRetry.Count);
            foreach (var record in toRetry)
            {
                if (string.IsNullOrWhiteSpace(record.HolderAbn)) continue;

                // Pending records are source items the first pass never reached; count each
                // as processed so the progress bar advances during a retry too. (Failed
                // records were already counted as processed in the original run.)
                var wasPending = record.EnrichmentStatus == EnrichmentStatus.Pending;

                record.EnrichmentStatus = EnrichmentStatus.Pending;
                work.Add(new WorkItem(record.HolderAbn!, byName ? record.BusinessName : null, record, CountAsProcessed: wasPending));
            }

            // ProcessedItems is a running counter that previously survived retries, so
            // re-counting the still-pending items on each retry pushed it past
            // TotalItems — one run reached 51,196/26,482 (193%), which pins the progress
            // bar and reads as "stuck" even while records are enriching fine. Re-derive
            // it from what's actually outstanding instead of carrying the old total
            // forward. Items that already failed were counted by the run that attempted
            // them; only the never-reached ones are still outstanding.
            run.ProcessedItems = Math.Max(0, run.TotalItems - work.Count(w => w.CountAsProcessed));
            await db.SaveChangesAsync(cancellationToken);

            if (!await EnrichAllAsync(run, work, byName, keywords, cancellationToken))
            {
                await StopAsCancelledAsync(run, cancellationToken);
                return;
            }

            run.Status = SearchRunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            await RecomputeCountersAsync(run, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await ContinueAfterEnrichmentAsync(run, cancellationToken);
        }
        // Shutdown mid-retry: same as ProcessAsync — let Hangfire re-queue and resume.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Retry for run {RunId} interrupted by shutdown; it will resume after restart.", searchRunId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Retry for run {RunId} failed.", searchRunId);
            run.Status = SearchRunStatus.Failed;
            run.Error = ex.Message;
            await RecomputeCountersAsync(run, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<string>> ResolveKeywordsAsync(SearchRun run, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(run.AppliedKeywords)
            ? await db.FilterKeywords.Where(k => k.IsActive).Select(k => k.Word).ToListAsync(cancellationToken)
            : run.AppliedKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private async Task<bool> IsCancelledAsync(Guid runId, CancellationToken cancellationToken) =>
        await db.SearchRuns.AsNoTracking()
            .Where(r => r.Id == runId).Select(r => r.CancellationRequested)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task StopAsCancelledAsync(SearchRun run, CancellationToken cancellationToken)
    {
        run.Status = SearchRunStatus.Cancelled;
        run.CompletedAt = DateTimeOffset.UtcNow;
        await RecomputeCountersAsync(run, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RecomputeCountersAsync(SearchRun run, CancellationToken cancellationToken)
    {
        run.FoundRecords = await db.BusinessNameRecords.CountAsync(r => r.SearchRunId == run.Id, cancellationToken);
        run.SuitableCount = await db.BusinessNameRecords.CountAsync(
            r => r.SearchRunId == run.Id && r.IsSuitable && r.EnrichmentStatus == EnrichmentStatus.Enriched, cancellationToken);
        run.ErrorCount = await db.BusinessNameRecords.CountAsync(
            r => r.SearchRunId == run.Id && r.EnrichmentStatus == EnrichmentStatus.Failed, cancellationToken);
    }

    /// <summary>
    /// What happens once ASIC/ABR enrichment finishes. When the run opted into the web
    /// stage, hand off to the (separate, durable) web-enrichment job — it auto-pushes
    /// itself at the end, so the discovered contact email rides along to Ontraport.
    /// Otherwise push straight away, as before.
    /// </summary>
    private async Task ContinueAfterEnrichmentAsync(SearchRun run, CancellationToken cancellationToken)
    {
        if (run.EnableWebEnrichment)
        {
            // Mark the web stage Queued so the run reads as still working (not "Completed")
            // from the moment ASIC finishes — the badge only reaches Completed once the web
            // job finishes. Clear any prior stop so an auto-run after a retry starts clean.
            run.WebEnrichmentState = WebEnrichmentRunState.Queued;
            run.WebEnrichmentCancellationRequested = false;
            await db.SaveChangesAsync(cancellationToken);

            jobs.EnqueueWebEnrichment(run.Id);
            return;
        }

        await AutoPushIfEnabledAsync(run, cancellationToken);
    }

    private async Task AutoPushIfEnabledAsync(SearchRun run, CancellationToken cancellationToken)
    {
        var config = await db.OntraportConfigurations.FirstOrDefaultAsync(cancellationToken);
        if (config?.AutoPushEnabled == true && ontraport.IsConfigured)
        {
            await PushInternalAsync(run, config.TagId, config.SequenceId, cancellationToken);
        }
    }

    private async Task PushInternalAsync(SearchRun run, int? tagId, int? sequenceId, CancellationToken cancellationToken)
    {
        if (!ontraport.IsConfigured)
        {
            logger.LogWarning("Ontraport not configured; skipping push for run {RunId}.", run.Id);
            return;
        }

        var records = await db.BusinessNameRecords
            .Where(r => r.SearchRunId == run.Id
                && r.IsSuitable
                && r.EnrichmentStatus == EnrichmentStatus.Enriched
                && r.OntraportStatus != OntraportPushStatus.Pushed)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            try
            {
                var contact = await ontraport.UpsertContactAsync(
                    new OntraportContactInput(
                        record.BusinessName, record.HolderName, record.HolderAbn,
                        record.AddressForServiceDocuments ?? record.PrincipalPlaceOfBusiness,
                        Email: record.ContactEmail),
                    cancellationToken);

                if (contact.Succeeded && contact.ContactId is not null)
                {
                    await ontraport.AddTagAsync(contact.ContactId, tagId, cancellationToken);
                    await ontraport.AddToSequenceAsync(contact.ContactId, sequenceId, cancellationToken);

                    record.OntraportStatus = OntraportPushStatus.Pushed;
                    record.OntraportContactId = contact.ContactId;
                    record.OntraportError = null;
                    record.PushedAt = DateTimeOffset.UtcNow;
                    run.PushedCount++;
                }
                else
                {
                    record.OntraportStatus = OntraportPushStatus.Failed;
                    record.OntraportError = contact.Error;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                record.OntraportStatus = OntraportPushStatus.Failed;
                record.OntraportError = ex.Message;
                logger.LogWarning(ex, "Ontraport push failed for record {RecordId}.", record.Id);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<AsicSearchResult> WithRetryAsync(
        Func<Task<AsicSearchResult>> operation, CancellationToken cancellationToken, int maxAttempts = 3)
    {
        var result = AsicSearchResult.Fail();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            result = await operation();
            if (result.Succeeded) return result;
            if (attempt < maxAttempts) await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
        }

        return result;
    }

    private static List<string> ParseAbns(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(['\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(NormalizeAbn)
                 .Where(abn => abn.Length > 0)
                 .Distinct().ToList();

    /// <summary>
    /// Reduce a pasted ABN entry to its digits. People paste them formatted —
    /// "23 967 785 990", "23-967-785-990", a spreadsheet cell with tabs, even an
    /// "ABN:" label — but ASIC's search matches only the bare number, so a
    /// space-formatted ABN silently returns no business names. Stripping to digits
    /// makes every reasonable paste work; a non-numeric entry collapses to empty and
    /// is dropped by the caller.
    /// </summary>
    private static string NormalizeAbn(string entry) =>
        new(entry.Where(char.IsDigit).ToArray());
}
