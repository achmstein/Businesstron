using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.RetryFailedEnrichment;

public record RetryFailedEnrichmentCommand(Guid SearchRunId) : IRequest;

public class RetryFailedEnrichmentCommandHandler(IApplicationDbContext context, IJobScheduler jobs)
    : IRequestHandler<RetryFailedEnrichmentCommand>
{
    public async Task Handle(RetryFailedEnrichmentCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.SearchRuns.AnyAsync(r => r.Id == request.SearchRunId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(SearchRun), request.SearchRunId);
        }

        jobs.EnqueueRetry(request.SearchRunId);
    }
}
