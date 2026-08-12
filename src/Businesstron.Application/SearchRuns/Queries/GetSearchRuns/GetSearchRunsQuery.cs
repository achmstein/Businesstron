using Businesstron.Application.Common.Interfaces;
using Businesstron.Application.Common.Models;
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

        var items = runs.Select(SearchRunDto.FromEntity).ToList();

        return new PaginatedList<SearchRunDto>(items, count, request.PageNumber, request.PageSize);
    }
}
