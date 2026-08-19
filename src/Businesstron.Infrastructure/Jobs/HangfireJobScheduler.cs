using Businesstron.Application.Common.Interfaces;
using Hangfire;

namespace Businesstron.Infrastructure.Jobs;

/// <summary>Enqueues background work on Hangfire. CancellationToken.None is replaced by Hangfire's job token at run time.</summary>
public sealed class HangfireJobScheduler(IBackgroundJobClient jobs) : IJobScheduler
{
    public void EnqueueProcess(Guid searchRunId) =>
        jobs.Enqueue<ISearchProcessingService>(x => x.ProcessAsync(searchRunId, CancellationToken.None));

    public void EnqueuePush(Guid searchRunId, bool onlyWithContact = false) =>
        jobs.Enqueue<ISearchProcessingService>(x => x.PushToOntraportAsync(searchRunId, onlyWithContact, CancellationToken.None));

    public void EnqueueRetry(Guid searchRunId) =>
        jobs.Enqueue<ISearchProcessingService>(x => x.RetryFailedAsync(searchRunId, CancellationToken.None));

    public void EnqueueWebEnrichment(Guid searchRunId) =>
        jobs.Enqueue<IWebEnrichmentService>(x => x.ProcessAsync(searchRunId, CancellationToken.None));
}
