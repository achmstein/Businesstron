using System.Globalization;
using System.Text;
using Businesstron.Application.Common.Interfaces;
using CsvHelper;

namespace Businesstron.Infrastructure.Csv;

public sealed class CsvExporter : ICsvExporter
{
    public async Task WriteAsync(
        IAsyncEnumerable<BusinessNameRecord> records,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        // leaveOpen: true — the response body stream is owned by the host, not us.
        await using var writer = new StreamWriter(destination, new UTF8Encoding(true), leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.Context.RegisterClassMap<BusinessNameRecordMap>();
        await csv.WriteRecordsAsync(records, cancellationToken);
    }
}
