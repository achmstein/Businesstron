using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Queries.GetSearchRun;

public class SearchRunDetailVm
{
    public SearchRunDto Run { get; init; } = null!;
    public IReadOnlyList<BusinessNameRecordDto> Records { get; init; } = [];

    /// <summary>1-based page of records contained in <see cref="Records"/>.</summary>
    public int RecordsPage { get; init; }
    public int RecordsPageSize { get; init; }

    /// <summary>Total records for the run (across all pages).</summary>
    public int RecordsTotalCount { get; init; }
    public int RecordsTotalPages { get; init; }

    /// <summary>Records still awaiting enrichment — reprocessed (with the failed ones) by Retry.</summary>
    public int PendingCount { get; init; }

    /// <summary>
    /// One representative enrichment-failure message for the whole run (not just the
    /// current page) so the warning banner can explain the cause without the client
    /// having to hold every record. Null when nothing failed.
    /// </summary>
    public string? SampleFailureReason { get; init; }
}

/// <param name="Filter">Optional stat-tile filter for the records table: "errors", "suitable" or "pushed".</param>
public record GetSearchRunQuery(Guid Id, int PageNumber = 1, int PageSize = 100, string? Filter = null) : IRequest<SearchRunDetailVm>;

public class GetSearchRunQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSearchRunQuery, SearchRunDetailVm>
{
    public async Task<SearchRunDetailVm> Handle(GetSearchRunQuery request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.Id);

        // A run can hold tens of thousands of records (large ABN lists). Never load them
        // all at once — page the query so the response and the client stay responsive.
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var recordsQuery = context.BusinessNameRecords
            .AsNoTracking()
            .Where(r => r.SearchRunId == request.Id);

        // The stat-tile filter narrows only the table (and its pagination). Run-wide
        // numbers (pending count, sample failure reason) keep using the base query.
        var filteredQuery = request.Filter?.ToLowerInvariant() switch
        {
            "errors" => recordsQuery.Where(r => r.EnrichmentStatus == EnrichmentStatus.Failed),
            "suitable" => recordsQuery.Where(r => r.IsSuitable && r.EnrichmentStatus == EnrichmentStatus.Enriched),
            "pushed" => recordsQuery.Where(r => r.OntraportStatus == OntraportPushStatus.Pushed),
            _ => recordsQuery
        };

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var pendingCount = await recordsQuery
            .CountAsync(r => r.EnrichmentStatus == EnrichmentStatus.Pending, cancellationToken);

        var records = await filteredQuery
            .OrderBy(r => r.BusinessName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var sampleFailureReason = run.ErrorCount > 0
            ? await recordsQuery
                .Where(r => r.EnrichmentStatus == EnrichmentStatus.Failed && r.EnrichmentError != null)
                .Select(r => r.EnrichmentError)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SearchRunDetailVm
        {
            Run = SearchRunDto.FromEntity(run),
            Records = records.Select(BusinessNameRecordDto.FromEntity).ToList(),
            RecordsPage = pageNumber,
            RecordsPageSize = pageSize,
            RecordsTotalCount = totalCount,
            RecordsTotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PendingCount = pendingCount,
            SampleFailureReason = sampleFailureReason
        };
    }
}
