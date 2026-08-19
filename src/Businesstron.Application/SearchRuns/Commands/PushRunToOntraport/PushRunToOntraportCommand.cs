using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Commands.PushRunToOntraport;

/// <param name="OnlyWithContact">Push only leads that have a contact email, rather than every suitable business.</param>
public record PushRunToOntraportCommand(Guid SearchRunId, bool OnlyWithContact = false) : IRequest;

public class PushRunToOntraportCommandHandler(IApplicationDbContext context, IJobScheduler jobs)
    : IRequestHandler<PushRunToOntraportCommand>
{
    public async Task Handle(PushRunToOntraportCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.SearchRuns
            .AnyAsync(r => r.Id == request.SearchRunId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException(nameof(SearchRun), request.SearchRunId);
        }

        jobs.EnqueuePush(request.SearchRunId, request.OnlyWithContact);
    }
}
