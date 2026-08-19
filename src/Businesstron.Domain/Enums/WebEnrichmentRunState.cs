namespace Businesstron.Domain.Enums;

/// <summary>
/// Run-level lifecycle of the optional web-enrichment stage — distinct from the
/// per-record <see cref="WebEnrichmentStatus"/>. Drives the stage-aware run badge and
/// the web Stop/Start control, and (with the heartbeat) tells a stuck job from a live one.
/// </summary>
public enum WebEnrichmentRunState
{
    /// <summary>The run did not opt into web enrichment.</summary>
    NotRequested = 0,

    /// <summary>Enqueued (auto after ASIC, or via the button) but not yet started.</summary>
    Queued = 1,

    /// <summary>A web-enrichment job is actively processing this run's records.</summary>
    Running = 2,

    /// <summary>Every eligible record has been through the web stage.</summary>
    Completed = 3,

    /// <summary>Stopped by the user before finishing (remaining records stay pending for a re-run).</summary>
    Cancelled = 4,

    /// <summary>Aborted by a provider/transport error (e.g. the consecutive-failure breaker tripped).</summary>
    Failed = 5
}
