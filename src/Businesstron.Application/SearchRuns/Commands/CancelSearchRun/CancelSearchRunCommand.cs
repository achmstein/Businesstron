using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.CancelSearchRun;

public record CancelSearchRunCommand(Guid SearchRunId) : IRequest;

public class CancelSearchRunCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CancelSearchRunCommand>
{
    public async Task Handle(CancelSearchRunCommand request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns
            .FirstOrDefaultAsync(r => r.Id == request.SearchRunId, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.SearchRunId);

        // Cooperative stop: the processor checks this flag between items and finishes as Cancelled.
        if (run.Status is SearchRunStatus.Pending or SearchRunStatus.Running)
        {
            run.CancellationRequested = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
