using Businesstron.Application.Common.Interfaces;
using Businesstron.Application.Common.Models;
using Businesstron.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Queries.GetSearchRuns;

public record GetSearchRunsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<SearchRunDto>>;

public class GetSearchRunsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSearchRunsQuery, PaginatedList<SearchRunDto>>
{
    public async Task<PaginatedList<SearchRunDto>> Handle(GetSearchRunsQuery request, CancellationToken cancellationToken)
    {
        var count = await context.SearchRuns.CountAsync(cancellationToken);

        var runs = await context.SearchRuns
            .AsNoTracking()
            .OrderByDescending(r => r.Created)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // One grouped pass for the web-enrichment counts of the runs on this page, so the
        // list can flag which runs have contact emails and which are still enriching —
        // without an N+1 count per row.
        var runIds = runs.Where(r => r.EnableWebEnrichment).Select(r => r.Id).ToList();
        var webByRun = new Dictionary<Guid, (int Emails, int Pending)>();
        if (runIds.Count > 0)
        {
            var grouped = await context.BusinessNameRecords
                .AsNoTracking()
                .Where(r => runIds.Contains(r.SearchRunId))
                .GroupBy(r => r.SearchRunId)
                .Select(g => new
                {
                    Id = g.Key,
                    Emails = g.Count(r => r.ContactEmail != null),
                    Pending = g.Count(r => r.WebEnrichmentStatus == WebEnrichmentStatus.Pending)
                })
                .ToListAsync(cancellationToken);

            webByRun = grouped.ToDictionary(x => x.Id, x => (x.Emails, x.Pending));
        }

        var items = runs
            .Select(r => webByRun.TryGetValue(r.Id, out var w)
                ? SearchRunDto.FromEntity(r, w.Emails, w.Pending)
                : SearchRunDto.FromEntity(r))
            .ToList();

        return new PaginatedList<SearchRunDto>(items, count, request.PageNumber, request.PageSize);
    }
}
