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

/// <param name="OnlySuitable">When true, exclude records the keyword filter flagged unsuitable.</param>
public record ExportSearchRunCsvQuery(Guid Id, bool OnlySuitable = false) : IRequest<CsvFileStream>;

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

        if (request.OnlySuitable)
        {
            query = query.Where(r => r.IsSuitable);
        }

        var records = query.OrderBy(r => r.BusinessName);
        var fileName = $"businesstron-{run.Created:yyyyMMdd-HHmmss}.csv";

        return new CsvFileStream(
            fileName,
            (destination, ct) => exporter.WriteAsync(records.AsAsyncEnumerable(), destination, ct));
    }
}
