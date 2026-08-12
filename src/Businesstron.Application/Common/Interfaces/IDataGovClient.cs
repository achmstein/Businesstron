namespace Businesstron.Application.Common.Interfaces;

/// <summary>A newly registered business name pulled from data.gov.au.</summary>
public sealed record DataGovRecord(string Name, string Abn);

/// <summary>Reads newly registered business names from the data.gov.au datastore.</summary>
public interface IDataGovClient
{
    Task<IReadOnlyList<DataGovRecord>> GetNewBusinessNamesAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
}
