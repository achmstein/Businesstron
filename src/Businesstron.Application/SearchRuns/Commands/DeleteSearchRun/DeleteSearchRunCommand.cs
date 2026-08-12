using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.DeleteSearchRun;

public record DeleteSearchRunCommand(Guid SearchRunId) : IRequest;

public class DeleteSearchRunCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteSearchRunCommand>
{
    public async Task Handle(DeleteSearchRunCommand request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns
            .FirstOrDefaultAsync(r => r.Id == request.SearchRunId, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.SearchRunId);

        // BusinessNameRecords are removed via the cascade-delete relationship.
        context.SearchRuns.Remove(run);
        await context.SaveChangesAsync(cancellationToken);
    }
}
