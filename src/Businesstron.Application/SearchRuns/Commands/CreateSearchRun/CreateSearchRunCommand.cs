using Businesstron.Application.Common.Interfaces;
using MediatR;

namespace Businesstron.Application.SearchRuns.Commands.CreateSearchRun;

public record CreateSearchRunCommand : IRequest<Guid>
{
    public SearchSource Source { get; init; }

    /// <summary>Required when <see cref="Source"/> is <see cref="SearchSource.DataGov"/>.</summary>
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    /// <summary>Required when <see cref="Source"/> is <see cref="SearchSource.AbnList"/>. One ABN per line.</summary>
    public string? AbnListRaw { get; init; }

    /// <summary>Optional per-run keyword override. When null/empty the active global keywords apply.</summary>
    public IReadOnlyList<string>? Keywords { get; init; }

    /// <summary>Optional labels to differentiate this run in the list.</summary>
    public IReadOnlyList<string>? Tags { get; init; }
}

public class CreateSearchRunCommandHandler(IApplicationDbContext context, IJobScheduler jobs, IUser user)
    : IRequestHandler<CreateSearchRunCommand, Guid>
{
    public async Task<Guid> Handle(CreateSearchRunCommand request, CancellationToken cancellationToken)
    {
        var appliedKeywords = request.Keywords?
            .Select(k => k.Trim())
            .Where(k => k.Length > 0)
            .ToList();

        var tags = request.Tags?
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();

        var run = new SearchRun
        {
            Source = request.Source,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AbnListRaw = request.AbnListRaw,
            AppliedKeywords = appliedKeywords is { Count: > 0 } ? string.Join(",", appliedKeywords) : null,
            Tags = tags is { Count: > 0 } ? string.Join(",", tags) : null,
            Status = SearchRunStatus.Pending,
            CreatedBy = user.Email ?? user.Id
        };

        context.SearchRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);

        jobs.EnqueueProcess(run.Id);

        return run.Id;
    }
}
