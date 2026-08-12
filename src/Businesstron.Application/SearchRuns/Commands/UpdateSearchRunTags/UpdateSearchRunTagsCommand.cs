using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.UpdateSearchRunTags;

public record UpdateSearchRunTagsCommand(Guid SearchRunId, IReadOnlyList<string> Tags) : IRequest;

public class UpdateSearchRunTagsCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateSearchRunTagsCommand>
{
    public async Task Handle(UpdateSearchRunTagsCommand request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns
            .FirstOrDefaultAsync(r => r.Id == request.SearchRunId, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.SearchRunId);

        var tags = request.Tags
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        run.Tags = tags.Count > 0 ? string.Join(",", tags) : null;
        await context.SaveChangesAsync(cancellationToken);
    }
}
