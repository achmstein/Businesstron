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

    /// <summary>Run-wide roll-up of the web-enrichment stage (drives its progress bar and filters).</summary>
    public WebEnrichmentSummaryDto Web { get; init; } = new();
}

/// <param name="Filter">
/// Optional stat-tile filter for the records table. ASIC tiles: "errors", "suitable",
/// "pushed". Web tiles: "websites", "emails", "webpending", "webfailed", "nowebsite".
/// </param>
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
            // Web-enrichment tiles — let the user jump straight to the enriched rows,
            // which are otherwise buried across hundreds of pages of ordered results.
            "websites" => recordsQuery.Where(r => r.Websites != null),
            "emails" => recordsQuery.Where(r => r.ContactEmail != null),
            "webpending" => recordsQuery.Where(r => r.WebEnrichmentStatus == WebEnrichmentStatus.Pending),
            "webfailed" => recordsQuery.Where(r => r.WebEnrichmentStatus == WebEnrichmentStatus.Failed),
            "nowebsite" => recordsQuery.Where(r => r.WebEnrichmentStatus == WebEnrichmentStatus.NoWebsite),
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

        // One grouped pass over the run's records for the web-stage roll-up (cheap: the
        // records are indexed by SearchRunId). Only meaningful when the run opted in.
        var web = new WebEnrichmentSummaryDto();
        if (run.EnableWebEnrichment)
        {
            var byStatus = (await recordsQuery
                    .GroupBy(r => r.WebEnrichmentStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken))
                .ToDictionary(x => x.Status, x => x.Count);

            int C(WebEnrichmentStatus s) => byStatus.TryGetValue(s, out var n) ? n : 0;

            web = new WebEnrichmentSummaryDto
            {
                NotAttempted = C(WebEnrichmentStatus.NotAttempted),
                Pending = C(WebEnrichmentStatus.Pending),
                Skipped = C(WebEnrichmentStatus.Skipped),
                NoWebsite = C(WebEnrichmentStatus.NoWebsite),
                NoEmail = C(WebEnrichmentStatus.NoEmail),
                Enriched = C(WebEnrichmentStatus.Enriched),
                Failed = C(WebEnrichmentStatus.Failed),
                WithWebsite = await recordsQuery.CountAsync(r => r.Websites != null, cancellationToken),
                WithEmail = await recordsQuery.CountAsync(r => r.ContactEmail != null, cancellationToken)
            };
        }

        return new SearchRunDetailVm
        {
            Run = SearchRunDto.FromEntity(run),
            Records = records.Select(BusinessNameRecordDto.FromEntity).ToList(),
            RecordsPage = pageNumber,
            RecordsPageSize = pageSize,
            RecordsTotalCount = totalCount,
            RecordsTotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PendingCount = pendingCount,
            SampleFailureReason = sampleFailureReason,
            Web = web
        };
    }
}
