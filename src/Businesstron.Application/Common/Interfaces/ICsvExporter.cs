namespace Businesstron.Application.Common.Interfaces;

/// <summary>Renders business-name records to the CSV layout the client already uses.</summary>
public interface ICsvExporter
{
    /// <summary>
    /// Streams the records as CSV straight into <paramref name="destination"/> (the HTTP
    /// response body) without buffering them all in memory, so large runs export cheaply.
    /// The destination stream is left open for the host to close.
    /// </summary>
    Task WriteAsync(
        IAsyncEnumerable<BusinessNameRecord> records,
        Stream destination,
        CancellationToken cancellationToken = default);
}
