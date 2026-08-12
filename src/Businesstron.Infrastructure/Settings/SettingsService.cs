using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Infrastructure.ExternalClients.Asic;
using Businesstron.Infrastructure.ExternalClients.Captcha;
using Businesstron.Infrastructure.Ontraport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.Settings;

/// <summary>
/// Reads integration credentials from the live configuration (via IOptionsMonitor,
/// so runtime overrides are reflected) and persists edits to a writable overrides
/// file. appsettings.json is never touched — it ships inside the read-only image;
/// the overrides file is layered on top and reloaded on change.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    // IOptionsMonitor (not IOptions) so reads reflect writes made below — IOptions
    // caches the value captured at startup and never sees reloadOnChange refreshes.
    private readonly IOptionsMonitor<OntraportOptions> _ontraport;
    private readonly IOptionsMonitor<TwoCaptchaOptions> _twoCaptcha;
    private readonly IOptionsMonitor<AsicOptions> _asic;
    private readonly string _overridesPath;

    public SettingsService(
        IOptionsMonitor<OntraportOptions> ontraport,
        IOptionsMonitor<TwoCaptchaOptions> twoCaptcha,
        IOptionsMonitor<AsicOptions> asic,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _ontraport = ontraport;
        _twoCaptcha = twoCaptcha;
        _asic = asic;

        // Match Program.cs: the writable overrides file lives outside the image.
        _overridesPath = configuration["Storage:OverridesPath"]
            ?? Path.Combine(environment.ContentRootPath, "settings.overrides.json");
    }

    public OntraportCredentials GetOntraportCredentials()
    {
        var o = _ontraport.CurrentValue;
        return new OntraportCredentials(o.AppId, o.ApiKey);
    }

    public Task UpdateOntraportCredentialsAsync(OntraportCredentials credentials, CancellationToken cancellationToken) =>
        UpdateSectionAsync(OntraportOptions.SectionName, new
        {
            AppId = credentials.AppId?.Trim(),
            ApiKey = credentials.ApiKey?.Trim(),
        }, cancellationToken);

    public TwoCaptchaCredentials GetTwoCaptchaCredentials()
    {
        var c = _twoCaptcha.CurrentValue;
        return new TwoCaptchaCredentials(
            c.ApiKey, c.DefaultTimeoutSeconds, c.RecaptchaTimeoutSeconds, c.PollingIntervalSeconds);
    }

    public Task UpdateTwoCaptchaCredentialsAsync(TwoCaptchaCredentials credentials, CancellationToken cancellationToken) =>
        UpdateSectionAsync(TwoCaptchaOptions.SectionName, new
        {
            ApiKey = credentials.ApiKey?.Trim(),
            credentials.DefaultTimeoutSeconds,
            credentials.RecaptchaTimeoutSeconds,
            credentials.PollingIntervalSeconds,
        }, cancellationToken);

    public AsicSettings GetAsicSettings()
    {
        var a = _asic.CurrentValue;
        return new AsicSettings(
            Math.Clamp(a.MaxConcurrency, 1, AsicSettings.MaxConcurrencyLimit),
            a.ForceTls13,
            a.ProxyUrl,
            a.ProxyUsername,
            a.ProxyPassword);
    }

    public Task UpdateAsicSettingsAsync(AsicSettings settings, CancellationToken cancellationToken) =>
        UpdateSectionAsync(AsicOptions.SectionName, new
        {
            // Clamp on save too — the endpoint validates, but the file is the source of truth.
            MaxConcurrency = Math.Clamp(settings.MaxConcurrency, 1, AsicSettings.MaxConcurrencyLimit),
            settings.ForceTls13,
            ProxyUrl = Blank(settings.ProxyUrl),
            ProxyUsername = Blank(settings.ProxyUsername),
            // Not trimmed — trailing whitespace can be part of a generated password.
            ProxyPassword = string.IsNullOrEmpty(settings.ProxyPassword) ? null : settings.ProxyPassword,
        }, cancellationToken);

    public string? ValidateAsicSettings(AsicSettings settings) =>
        AsicProxy.TryCreate(ToOptions(settings), out _, out var error) ? null : error;

    public Task<AsicConnectionTestResult> TestAsicConnectionAsync(
        AsicSettings settings, CancellationToken cancellationToken) =>
        AsicConnectionTester.TestAsync(ToOptions(settings), cancellationToken);

    /// <summary>
    /// Overlays the supplied proxy fields onto the live options, so a test reflects
    /// what the admin has typed while still using the configured base URL and headers.
    /// </summary>
    private AsicOptions ToOptions(AsicSettings settings)
    {
        var current = _asic.CurrentValue;
        return new AsicOptions
        {
            BaseUrl = current.BaseUrl,
            RecaptchaSiteKey = current.RecaptchaSiteKey,
            UserAgent = current.UserAgent,
            TimeoutSeconds = current.TimeoutSeconds,
            MaxConcurrency = current.MaxConcurrency,
            ForceTls13 = settings.ForceTls13,
            ProxyUrl = Blank(settings.ProxyUrl),
            ProxyUsername = Blank(settings.ProxyUsername),
            ProxyPassword = settings.ProxyPassword,
        };
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task UpdateSectionAsync(string sectionName, object section, CancellationToken cancellationToken)
    {
        // Load existing overrides if present, else start fresh. Merge at the section
        // level so unrelated sections (written by other Save actions) are preserved.
        JsonObject root;
        if (File.Exists(_overridesPath))
        {
            var existing = await File.ReadAllTextAsync(_overridesPath, cancellationToken);
            root = JsonNode.Parse(existing) as JsonObject ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
            var dir = Path.GetDirectoryName(_overridesPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        var sectionJson = JsonSerializer.Serialize(section);
        root[sectionName] = JsonNode.Parse(sectionJson);

        var writeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        await File.WriteAllTextAsync(_overridesPath, JsonSerializer.Serialize(root, writeOptions), cancellationToken);
    }
}
