namespace Businesstron.Domain.Entities;

/// <summary>
/// One search job. Replaces the old console <c>Worker</c> run: it captures the
/// requested source and range, tracks progress, and owns the resulting records.
/// </summary>
public class SearchRun : BaseAuditableEntity
{
    public SearchSource Source { get; set; }

    /// <summary>Inclusive start date (data.gov.au source only).</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Inclusive end date (data.gov.au source only).</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Raw ABN list as pasted by the user (AbnList source only).</summary>
    public string? AbnListRaw { get; set; }

    /// <summary>
    /// Comma-separated keyword snapshot chosen for this run. When null, the active
    /// global keywords apply. Recording per-run keeps each run's filter reproducible.
    /// </summary>
    public string? AppliedKeywords { get; set; }

    /// <summary>Optional comma-separated labels so the admin can tell runs apart (e.g. "NSW, plumbers").</summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Opt-in: after ASIC/ABR enrichment, continue into the web stage (reverse-whois →
    /// auda email → contact) for suitable records whose renewal is within the lead
    /// window. Off by default because each record spends whois + CAPTCHA credits.
    /// </summary>
    public bool EnableWebEnrichment { get; set; }

    public SearchRunStatus Status { get; set; } = SearchRunStatus.Pending;

    /// <summary>Set by the user to stop a running search; the processor checks this cooperatively.</summary>
    public bool CancellationRequested { get; set; }

    /// <summary>Number of source items (records from data.gov.au, or ABNs) to process.</summary>
    public int TotalItems { get; set; }

    /// <summary>Number of source items processed so far.</summary>
    public int ProcessedItems { get; set; }

    /// <summary>Number of resulting business-name rows found.</summary>
    public int FoundRecords { get; set; }

    public int SuitableCount { get; set; }

    public int PushedCount { get; set; }

    public int ErrorCount { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? Error { get; set; }

    public ICollection<BusinessNameRecord> Records { get; set; } = new List<BusinessNameRecord>();
}
