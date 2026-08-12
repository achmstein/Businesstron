using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.FilterKeywords.Queries.GetFilterKeywords;

public record FilterKeywordDto(Guid Id, string Word, bool IsActive);

public record GetFilterKeywordsQuery : IRequest<IReadOnlyList<FilterKeywordDto>>;

public class GetFilterKeywordsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFilterKeywordsQuery, IReadOnlyList<FilterKeywordDto>>
{
    public async Task<IReadOnlyList<FilterKeywordDto>> Handle(GetFilterKeywordsQuery request, CancellationToken cancellationToken)
    {
        return await context.FilterKeywords
            .AsNoTracking()
            .OrderBy(k => k.Word)
            .Select(k => new FilterKeywordDto(k.Id, k.Word, k.IsActive))
            .ToListAsync(cancellationToken);
    }
}
