using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Businesstron.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.ExternalClients.WhoisXml;

/// <summary>
/// Reverse-WHOIS via the WhoisXML API. Given an ABN/ACN (which .au registrants carry
/// in their WHOIS records) it returns the matching domains. Ports the console
/// ReverseWhois worker, but builds a correct request body and reads the API key from
/// the live options so a Settings edit applies without a restart.
/// </summary>
public sealed class WhoisXmlReverseWhoisClient(
    HttpClient http, IOptionsMonitor<WhoisXmlOptions> options, ILogger<WhoisXmlReverseWhoisClient> logger)
    : IReverseWhoisClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private WhoisXmlOptions Options => options.CurrentValue;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Options.ApiKey);

    public async Task<IReadOnlyList<string>> FindDomainsAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var opts = Options;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            throw new InvalidOperationException(
                "No WhoisXML API key is configured. Set it in Settings → Reverse WHOIS.");
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        var request = new
        {
            apiKey = opts.ApiKey,
            searchType = opts.SearchType,
            mode = opts.Mode,
            punycode = true,
            responseFormat = "json",
            basicSearchTerms = new
            {
                include = new[] { searchTerm.Trim() },
                exclude = Array.Empty<string>()
            }
        };

        using var response = await http.PostAsJsonAsync(opts.BaseUrl, request, Json, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReverseWhoisResponse>(Json, cancellationToken);

        if (payload is null)
        {
            return [];
        }

        var domains = payload.DomainsList?
            .Select(NormaliseDomain)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        logger.LogDebug("Reverse-whois for {Term} returned {Count} domain(s).", searchTerm, domains.Count);
        return domains;
    }

    /// <summary>The API returns bare domains, but trim any scheme/path just in case.</summary>
    private static string NormaliseDomain(string raw)
    {
        var d = raw.Trim();
        var scheme = d.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0) d = d[(scheme + 3)..];
        var slash = d.IndexOf('/');
        if (slash >= 0) d = d[..slash];
        return d.Trim().TrimEnd('.');
    }

    private sealed record ReverseWhoisResponse(
        [property: JsonPropertyName("domainsList")] List<string>? DomainsList,
        [property: JsonPropertyName("domainsCount")] int? DomainsCount);
}
