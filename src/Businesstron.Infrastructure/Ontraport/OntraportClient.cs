using System.Text.Json;
using Businesstron.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.Ontraport;

/// <summary>
/// Minimal Ontraport REST client. Creates a contact from a business-name lead and,
/// optionally, applies a tag / subscribes it to a sequence so the client's existing
/// Ontraport automation (email/mail) takes over.
///
/// NOTE: ASIC records carry no email, so contacts are created (not merged on email);
/// field mapping is intentionally conservative and should be tuned to David's account.
/// </summary>
public sealed class OntraportClient : IOntraportClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OntraportClient> _logger;
    private readonly IOptionsMonitor<OntraportOptions> _options;

    public OntraportClient(HttpClient http, IOptionsMonitor<OntraportOptions> options, ILogger<OntraportClient> logger)
    {
        _options = options;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(options.CurrentValue.BaseUrl);
        // Auth headers are attached per-request (see AddAuth) rather than as shared
        // defaults, so credentials edited from the Settings UI take effect immediately.
    }

    // CurrentValue so credential edits made from the Settings UI are seen without restart.
    private OntraportOptions Options => _options.CurrentValue;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Options.AppId) && !string.IsNullOrWhiteSpace(Options.ApiKey);

    private void AddAuth(HttpRequestMessage request)
    {
        var options = Options;
        request.Headers.TryAddWithoutValidation("Api-Appid", options.AppId);
        request.Headers.TryAddWithoutValidation("Api-Key", options.ApiKey);
    }

    public async Task<OntraportContactResult> UpsertContactAsync(OntraportContactInput input, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return new OntraportContactResult(false, null, "Ontraport API credentials are not configured.");
        }

        var (firstName, lastName) = SplitName(input.HolderName);

        var fields = new List<KeyValuePair<string, string>>
        {
            new("company", input.BusinessName ?? string.Empty),
            new("firstname", firstName),
            new("lastname", lastName),
        };

        if (!string.IsNullOrWhiteSpace(input.Address)) fields.Add(new("address", input.Address));
        if (!string.IsNullOrWhiteSpace(input.Email)) fields.Add(new("email", input.Email));
        if (!string.IsNullOrWhiteSpace(input.Abn)) fields.Add(new("office_phone", input.Abn)); // ABN parked here until a custom field id is provided

        try
        {
            using var content = new FormUrlEncodedContent(fields);
            using var request = new HttpRequestMessage(HttpMethod.Post, "Contacts") { Content = content };
            AddAuth(request);
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new OntraportContactResult(false, null, $"Ontraport returned {(int)response.StatusCode}: {Truncate(body)}");
            }

            var id = ExtractContactId(body);
            return id is null
                ? new OntraportContactResult(false, null, $"Could not read contact id from response: {Truncate(body)}")
                : new OntraportContactResult(true, id, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ontraport upsert failed for {Company}.", input.BusinessName);
            return new OntraportContactResult(false, null, ex.Message);
        }
    }

    public async Task AddTagAsync(string contactId, int? tagId, CancellationToken cancellationToken)
    {
        if (tagId is null || !IsConfigured)
        {
            return;
        }

        // PUT /objects/tag — objectID 0 = Contacts.
        var fields = new[]
        {
            new KeyValuePair<string, string>("objectID", "0"),
            new("ids[]", contactId),
            new("add_list[]", tagId.Value.ToString()),
        };

        using var content = new FormUrlEncodedContent(fields);
        using var request = new HttpRequestMessage(HttpMethod.Put, "objects/tag") { Content = content };
        AddAuth(request);
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddToSequenceAsync(string contactId, int? sequenceId, CancellationToken cancellationToken)
    {
        if (sequenceId is null || !IsConfigured)
        {
            return;
        }

        // PUT /objects/subscribe — subscribe contact(s) to a sequence.
        var fields = new[]
        {
            new KeyValuePair<string, string>("objectID", "0"),
            new("ids[]", contactId),
            new("add_list[]", sequenceId.Value.ToString()),
            new("sub_type", "Sequence"),
        };

        using var content = new FormUrlEncodedContent(fields);
        using var request = new HttpRequestMessage(HttpMethod.Put, "objects/subscribe") { Content = content };
        AddAuth(request);
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static (string First, string Last) SplitName(string? holderName)
    {
        if (string.IsNullOrWhiteSpace(holderName))
        {
            return (string.Empty, string.Empty);
        }

        var parts = holderName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 1 ? (parts[0], string.Empty) : (parts[0], parts[1]);
    }

    private static string? ExtractContactId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                // data can be an object ({id:...}) or, for some endpoints, an array.
                var element = data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0 ? data[0] : data;
                if (element.TryGetProperty("id", out var idProp))
                {
                    return idProp.ValueKind == JsonValueKind.Number ? idProp.GetRawText() : idProp.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }

    private static string Truncate(string value) => value.Length <= 300 ? value : value[..300];
}
