using System.Text.Json.Serialization;

namespace Businesstron.Infrastructure.ExternalClients.DataGov;

// Shapes for the data.gov.au datastore_search_sql response.
internal sealed class QueryResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public QueryResult? Result { get; set; }
}

internal sealed class QueryResult
{
    [JsonPropertyName("records")]
    public List<QueryRecord>? Records { get; set; }
}

internal sealed class QueryRecord
{
    [JsonPropertyName("BN_NAME")]
    public string? BnName { get; set; }

    [JsonPropertyName("BN_ABN")]
    public string? BnAbn { get; set; }
}
