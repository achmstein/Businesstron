using System.Text.Json;
using Businesstron.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.ExternalClients.DataGov;

/// <summary>Reads newly registered business names from the data.gov.au datastore SQL API.</summary>
public sealed class DataGovClient(HttpClient http, IOptions<DataGovOptions> options) : IDataGovClient
{
    private readonly DataGovOptions _options = options.Value;

    public async Task<IReadOnlyList<DataGovRecord>> GetNewBusinessNamesAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        var result = new List<DataGovRecord>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sql = $"SELECT \"BN_NAME\", \"BN_ABN\" from \"{_options.ResourceId}\" " +
                      $"WHERE \"BN_REG_DT\" = '{date:dd/MM/yyyy}' AND \"BN_ABN\" IS NOT NULL";

            var content = await http.GetStringAsync($"datastore_search_sql?sql={sql}", cancellationToken);

            var response = JsonSerializer.Deserialize<QueryResponse>(content);

            if (response?.Result?.Records is { } records)
            {
                result.AddRange(records
                    .Where(r => !string.IsNullOrWhiteSpace(r.BnAbn))
                    .Select(r => new DataGovRecord(r.BnName ?? string.Empty, r.BnAbn!)));
            }
        }

        return result;
    }
}
