namespace Businesstron.Application.SearchRuns.Queries;

/// <summary>
/// Run-wide roll-up of the optional web-enrichment stage (reverse-whois → auda email),
/// aggregated over every record so the UI can show a live progress bar and let the user
/// jump straight to the enriched rows — which are otherwise scattered across hundreds of
/// pages of results.
/// </summary>
public class WebEnrichmentSummaryDto
{
    /// <summary>Run-level lifecycle of the stage: NotRequested / Queued / Running / Completed / Cancelled / Failed.</summary>
    public string State { get; init; } = nameof(Domain.Enums.WebEnrichmentRunState.NotRequested);

    /// <summary>A worker is genuinely processing right now (Running with a recent heartbeat) — drives Stop vs Start.</summary>
    public bool Active { get; init; }

    /// <summary>A stop was requested and is being honoured (Active and cancellation pending).</summary>
    public bool Stopping { get; init; }

    /// <summary>Run-level web error (e.g. the failure breaker tripped), if the stage failed.</summary>
    public string? Error { get; init; }

    /// <summary>Never reached (run finished before the web stage got to them, or it never ran).</summary>
    public int NotAttempted { get; init; }

    /// <summary>Queued for the web stage and not yet processed — while &gt; 0 the stage is still running.</summary>
    public int Pending { get; init; }

    /// <summary>Out of the renewal window (or unsuitable): deliberately not web-enriched.</summary>
    public int Skipped { get; init; }

    /// <summary>Processed, but reverse-whois found no domain for the holder.</summary>
    public int NoWebsite { get; init; }

    /// <summary>Domain(s) found but no contact email surfaced on auda.</summary>
    public int NoEmail { get; init; }

    /// <summary>At least a website (usually an email too) was found.</summary>
    public int Enriched { get; init; }

    /// <summary>A transport/provider error stopped the web lookup for this record.</summary>
    public int Failed { get; init; }

    /// <summary>Records with at least one website (a superset of <see cref="Enriched"/>).</summary>
    public int WithWebsite { get; init; }

    /// <summary>Records with at least one contact email.</summary>
    public int WithEmail { get; init; }

    /// <summary>Records that entered (or are still in) the web pipeline — the progress-bar denominator.</summary>
    public int Eligible => Pending + Processed;

    /// <summary>Records the web stage has finished with (any terminal outcome).</summary>
    public int Processed => NoWebsite + NoEmail + Enriched + Failed;

    /// <summary>True while records are still queued (outstanding work exists).</summary>
    public bool Running => Pending > 0;

    /// <summary>True when the run has any web-enrichment activity at all (so the UI can show the section).</summary>
    public bool HasActivity => Eligible > 0 || Skipped > 0
        || State != nameof(Domain.Enums.WebEnrichmentRunState.NotRequested);
}
