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

        if (!response.IsSuccessStatusCode)
        {
            // WhoisXML returns a JSON body explaining the failure (e.g. 403 "Access
            // restricted. Reasons: insufficient credits balance, incorrect API key, or your
            // IP is not on the allow-list."). Surface that instead of the framework's
            // generic "Response status code does not indicate success" so a blocked run is
            // self-explanatory in the UI rather than needing a log dive.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = ExtractApiError(body);
            var message = detail is not null
                ? $"WhoisXML reverse-whois error ({(int)response.StatusCode}): {detail}"
                : $"WhoisXML reverse-whois returned {(int)response.StatusCode} {response.ReasonPhrase}.";
            logger.LogWarning("Reverse-whois failed for {Term}: {Message}", searchTerm, message);
            throw new InvalidOperationException(message);
        }

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

    /// <summary>
    /// Pulls the human-readable reason out of a WhoisXML error body
    /// (<c>{"code":403,"messages":"…"}</c>), tolerating a string or array message and
    /// falling back to a trimmed snippet for a non-JSON body. Null when nothing usable.
    /// </summary>
    private static string? ExtractApiError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            foreach (var field in (ReadOnlySpan<string>)["messages", "message", "error"])
            {
                if (root.TryGetProperty(field, out var v))
                {
                    return v.ValueKind switch
                    {
                        JsonValueKind.String => v.GetString(),
                        JsonValueKind.Array => string.Join("; ",
                            v.EnumerateArray().Select(e => e.GetString() ?? e.ToString())),
                        _ => v.ToString()
                    };
                }
            }

            return null;
        }
        catch (JsonException)
        {
            var trimmed = body.Trim();
            return trimmed.Length > 200 ? trimmed[..200] : trimmed;
        }
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
