namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// The background pipeline. Implemented in Infrastructure and invoked by Hangfire.
/// Ports the old console <c>Worker</c> orchestration to a durable job.
/// </summary>
public interface ISearchProcessingService
{
    /// <summary>Run the full pipeline for a search run: fetch source → ASIC/ABR lookups → filter → auto-push.</summary>
    Task ProcessAsync(Guid searchRunId, CancellationToken cancellationToken);

    /// <summary>Push a run's suitable, not-yet-pushed records to Ontraport.</summary>
    Task PushToOntraportAsync(Guid searchRunId, bool onlyWithContact, CancellationToken cancellationToken);

    /// <summary>Re-run enrichment for a run's failed/pending records.</summary>
    Task RetryFailedAsync(Guid searchRunId, CancellationToken cancellationToken);
}
