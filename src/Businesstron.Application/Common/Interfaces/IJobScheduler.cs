namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the background job queue (Hangfire in Infrastructure), so the
/// Application layer can enqueue long-running work without depending on Hangfire.
/// </summary>
public interface IJobScheduler
{
    /// <summary>Enqueue the full pipeline for a run: search → filter → auto-push.</summary>
    void EnqueueProcess(Guid searchRunId);

    /// <summary>Enqueue an Ontraport push for a run's suitable, not-yet-pushed records.</summary>
    void EnqueuePush(Guid searchRunId);

    /// <summary>Enqueue a re-enrichment pass over a run's failed/pending records.</summary>
    void EnqueueRetry(Guid searchRunId);

    /// <summary>Enqueue the web stage for a run: reverse-whois → auda email → contact.</summary>
    void EnqueueWebEnrichment(Guid searchRunId);
}
