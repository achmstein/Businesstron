namespace Businesstron.Application.Common.Interfaces;

/// <summary>Ontraport API credentials, editable from the Settings UI.</summary>
public sealed record OntraportCredentials(string? AppId, string? ApiKey);

/// <summary>2Captcha API credentials and solve tuning, editable from the Settings UI.</summary>
public sealed record TwoCaptchaCredentials(
    string? ApiKey,
    int DefaultTimeoutSeconds,
    int RecaptchaTimeoutSeconds,
    int PollingIntervalSeconds);

/// <summary>WhoisXML (reverse-WHOIS) API key, editable from the Settings UI.</summary>
public sealed record WhoisXmlCredentials(string? ApiKey);

/// <summary>ASIC enrichment tuning and connection routing, editable from the Settings UI.</summary>
/// <param name="MaxConcurrency">Parallel ASIC sessions per run.</param>
/// <param name="ForceTls13">Advertise TLS 1.3 only, so Cloudflare doesn't fingerprint the handshake as a bot.</param>
/// <param name="ProxyUrl">Outbound proxy address, or null to connect directly.</param>
/// <param name="ProxyUsername">Proxy username, or null when the proxy is open or credentials are embedded in the address.</param>
/// <param name="ProxyPassword">Proxy password, paired with <paramref name="ProxyUsername"/>.</param>
public sealed record AsicSettings(
    int MaxConcurrency,
    bool ForceTls13 = true,
    string? ProxyUrl = null,
    string? ProxyUsername = null,
    string? ProxyPassword = null)
{
    /// <summary>
    /// Hard upper bound on parallel ASIC sessions, enforced on save and again at run
    /// time, so the setting can never exhaust the server (or hammer ASIC into
    /// throttling the IP).
    /// </summary>
    public const int MaxConcurrencyLimit = 16;
}

/// <summary>Outcome of a one-request probe against the ASIC registry.</summary>
/// <param name="Succeeded">True only when ASIC answered 200.</param>
/// <param name="StatusCode">The HTTP status ASIC returned, or null if the request never completed.</param>
/// <param name="Message">Admin-facing explanation, including whether the proxy was used.</param>
public sealed record AsicConnectionTestResult(bool Succeeded, int? StatusCode, string Message);

/// <summary>
/// Reads and persists the integration credentials that used to be server-only
/// (appsettings/environment). Reads reflect the live configuration (including
/// runtime overrides); writes are persisted to a writable overrides file that the
/// configuration system reloads, so changes take effect without a restart.
/// </summary>
public interface ISettingsService
{
    OntraportCredentials GetOntraportCredentials();

    Task UpdateOntraportCredentialsAsync(OntraportCredentials credentials, CancellationToken cancellationToken);

    TwoCaptchaCredentials GetTwoCaptchaCredentials();

    Task UpdateTwoCaptchaCredentialsAsync(TwoCaptchaCredentials credentials, CancellationToken cancellationToken);

    WhoisXmlCredentials GetWhoisXmlCredentials();

    Task UpdateWhoisXmlCredentialsAsync(WhoisXmlCredentials credentials, CancellationToken cancellationToken);

    AsicSettings GetAsicSettings();

    Task UpdateAsicSettingsAsync(AsicSettings settings, CancellationToken cancellationToken);

    /// <summary>Returns null when the proxy in <paramref name="settings"/> is usable, else the reason.</summary>
    string? ValidateAsicSettings(AsicSettings settings);

    /// <summary>
    /// Probes ASIC once using <paramref name="settings"/> without saving them, so a
    /// proxy can be verified before a run depends on it.
    /// </summary>
    Task<AsicConnectionTestResult> TestAsicConnectionAsync(AsicSettings settings, CancellationToken cancellationToken);
}
