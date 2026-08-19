using Businesstron.Application.Common.Exceptions;
using Businesstron.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.SearchRuns.Queries.ExportSearchRunCsv;

/// <summary>
/// A CSV download that writes straight to the response stream. <see cref="WriteTo"/> is
/// invoked by the endpoint during response rendering, while the request scope (and its
/// DbContext) is still alive, so the records are streamed rather than buffered.
/// </summary>
public sealed record CsvFileStream(string FileName, Func<Stream, CancellationToken, Task> WriteTo);

/// <param name="Filter">
/// Which records to include: "suitable" (keyword-filter passed), "contacts" (has a website
/// or a contact email), or anything else / null for all records.
/// </param>
public record ExportSearchRunCsvQuery(Guid Id, string? Filter = null) : IRequest<CsvFileStream>;

public class ExportSearchRunCsvQueryHandler(IApplicationDbContext context, ICsvExporter exporter)
    : IRequestHandler<ExportSearchRunCsvQuery, CsvFileStream>
{
    public async Task<CsvFileStream> Handle(ExportSearchRunCsvQuery request, CancellationToken cancellationToken)
    {
        var run = await context.SearchRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SearchRun), request.Id);

        var query = context.BusinessNameRecords
            .AsNoTracking()
            .Where(r => r.SearchRunId == request.Id);

        var filter = request.Filter?.ToLowerInvariant();
        query = filter switch
        {
            "suitable" => query.Where(r => r.IsSuitable),
            "contacts" => query.Where(r => r.Websites != null || r.ContactEmail != null),
            _ => query
        };

        var records = query.OrderBy(r => r.BusinessName);
        var suffix = filter is "suitable" or "contacts" ? $"-{filter}" : "";
        var fileName = $"businesstron-{run.Created:yyyyMMdd-HHmmss}{suffix}.csv";

        return new CsvFileStream(
            fileName,
            (destination, ct) => exporter.WriteAsync(records.AsAsyncEnumerable(), destination, ct));
    }
}
