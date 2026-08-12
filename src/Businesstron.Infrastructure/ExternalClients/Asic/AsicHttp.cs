using System.Net;
using System.Net.Security;
using System.Security.Authentication;

namespace Businesstron.Infrastructure.ExternalClients.Asic;

/// <summary>
/// Builds the HTTP transport every ASIC request goes through. Shared by the scraper and
/// the Settings connection test so the test exercises the identical stack — a test that
/// used different TLS settings would report a reachability the real run doesn't have.
/// </summary>
public static class AsicHttp
{
    /// <summary>
    /// Creates a handler with its own cookie jar (the scraper's ADF session lives in
    /// cookies, so instances must not share one).
    /// </summary>
    public static SocketsHttpHandler CreateHandler(AsicOptions options)
    {
        var handler = new SocketsHttpHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        };

        // ASIC is behind Cloudflare, which fingerprints the TLS handshake. A default
        // .NET ClientHello (offering TLS 1.2 alongside 1.3) is classified as a bot and
        // answered 403 for the whole domain, before any request is read — measured on
        // the deployed host at 1/10 success. Advertising TLS 1.3 only matches what a
        // browser negotiates and restores 10/10. Keep this on unless ASIC drops TLS 1.3.
        if (options.ForceTls13)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls13,
            };
        }

        // A bad address fails loudly rather than silently falling back to a direct
        // connection, which would quietly defeat the point of configuring a proxy.
        if (!AsicProxy.TryCreate(options, out var proxy, out var proxyError))
        {
            handler.Dispose();
            throw new InvalidOperationException($"The ASIC proxy is misconfigured. {proxyError}");
        }

        if (proxy is not null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        return handler;
    }
}
