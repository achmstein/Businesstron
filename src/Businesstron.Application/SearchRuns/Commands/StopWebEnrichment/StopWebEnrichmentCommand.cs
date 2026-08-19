using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.StopWebEnrichment;

/// <summary>
/// Requests a cooperative stop of the web-enrichment stage — the web "Stop" button. The
/// running job checks the flag between save batches and finishes as Cancelled, leaving the
/// remaining records Pending so a later re-run continues from there. Distinct from the ABN
/// search's Stop, which only affects the ASIC stage.
/// </summary>
public record StopWebEnrichmentCommand(Guid SearchRunId) : IRequest;

public class StopWebEnrichmentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<StopWebEnrichmentCommand>
{
    public async Task Handle(StopWebEnrichmentCommand request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns
            .FirstOrDefaultAsync(r => r.Id == request.SearchRunId, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.SearchRunId);

        // Only meaningful while the stage is queued or running.
        if (run.WebEnrichmentState is WebEnrichmentRunState.Queued or WebEnrichmentRunState.Running)
        {
            run.WebEnrichmentCancellationRequested = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
