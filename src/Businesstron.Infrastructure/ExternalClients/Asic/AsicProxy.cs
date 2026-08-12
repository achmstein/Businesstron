using System.Net;

namespace Businesstron.Infrastructure.ExternalClients.Asic;

/// <summary>
/// Builds the outbound proxy for ASIC requests from configuration. Shared by the
/// registry client, the connection test and the settings endpoint, so an address that
/// saves is an address that the scraper will actually dial.
/// </summary>
public static class AsicProxy
{
    // SocketsHttpHandler dials SOCKS as well as HTTP proxies, which matters because
    // most residential providers hand out one or the other, not both.
    private static readonly string[] SupportedSchemes = ["http", "https", "socks4", "socks4a", "socks5"];

    /// <summary>
    /// Creates the proxy described by <paramref name="options"/>, if any. A blank
    /// address is valid and yields a null proxy (connect directly); an unusable one
    /// returns false with the reason in <paramref name="error"/>.
    /// </summary>
    public static bool TryCreate(AsicOptions options, out IWebProxy? proxy, out string? error)
    {
        proxy = null;
        error = null;

        var address = options.ProxyUrl?.Trim();
        if (string.IsNullOrEmpty(address))
        {
            return true;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            error = "The proxy address must be an absolute URL, for example http://host:port.";
            return false;
        }

        if (!SupportedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            error = $"Proxy scheme '{uri.Scheme}' is not supported. Use one of: {string.Join(", ", SupportedSchemes)}.";
            return false;
        }

        var username = options.ProxyUsername?.Trim();
        var credentials = !string.IsNullOrEmpty(username)
            ? new NetworkCredential(username, options.ProxyPassword ?? string.Empty)
            : CredentialsFromUserInfo(uri);

        // WebProxy wants a bare address; strip any user:pass@ now that it's been read.
        var web = new WebProxy(StripUserInfo(uri)) { BypassProxyOnLocal = false };

        if (credentials is not null)
        {
            web.Credentials = credentials;
        }

        proxy = web;
        return true;
    }

    /// <summary>Providers commonly hand out a single http://user:pass@host:port string.</summary>
    private static NetworkCredential? CredentialsFromUserInfo(Uri uri)
    {
        if (string.IsNullOrEmpty(uri.UserInfo))
        {
            return null;
        }

        var parts = uri.UserInfo.Split(':', 2);
        return new NetworkCredential(
            Uri.UnescapeDataString(parts[0]),
            parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty);
    }

    private static Uri StripUserInfo(Uri uri) =>
        string.IsNullOrEmpty(uri.UserInfo)
            ? uri
            : new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty }.Uri;
}
