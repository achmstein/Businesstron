using System.Net;
using Businesstron.Application.Common.Interfaces;

namespace Businesstron.Infrastructure.ExternalClients.Asic;

/// <summary>
/// Single-request reachability probe for the ASIC registry. Backs the Settings page's
/// "Test connection" so a proxy is proven before a run commits tens of thousands of
/// requests to it — and so a Cloudflare block is visible in one click instead of being
/// inferred from a run that fails every record.
/// </summary>
public static class AsicConnectionTester
{
    // Format-valid but unallocated: we only care about the HTTP status, not the result,
    // and this keeps the probe off any real company's record.
    private const string ProbeAbn = "12345678901";

    public static async Task<AsicConnectionTestResult> TestAsync(
        AsicOptions options, CancellationToken cancellationToken)
    {
        SocketsHttpHandler handler;
        try
        {
            // Identical transport to the scraper's, so a pass here means a run passes.
            handler = AsicHttp.CreateHandler(options);
        }
        catch (InvalidOperationException ex)
        {
            return new AsicConnectionTestResult(false, null, ex.Message);
        }

        using var http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(options.BaseUrl),
            // Deliberately shorter than the scraper's timeout — this runs while an admin
            // waits on a button, so it should fail fast rather than hang for minutes.
            Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 45)),
        };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);

        var via = string.IsNullOrWhiteSpace(options.ProxyUrl) ? "connecting directly" : "via the proxy";
        var tls = options.ForceTls13 ? string.Empty : " Try turning TLS 1.3 back on.";

        try
        {
            using var response = await http.GetAsync(
                $"RegistrySearch/faces/landing/panelSearch.jspx?searchType=Bn&searchName=&searchNumber={ProbeAbn}",
                cancellationToken);

            var status = (int)response.StatusCode;

            return status switch
            {
                200 => new AsicConnectionTestResult(true, status, $"ASIC responded 200 OK {via}. Enrichment should work."),
                // Cloudflare answers 403 to a handshake it fingerprints as automation —
                // it is the client's TLS profile that is refused, not the server's IP.
                403 => new AsicConnectionTestResult(false, status,
                    $"ASIC returned 403 {via} — Cloudflare rejected the connection as automated traffic.{tls}"),
                429 => new AsicConnectionTestResult(false, status,
                    $"ASIC returned 429 {via} — requests are being rate-limited. Lower the parallel session count or wait before running."),
                _ => new AsicConnectionTestResult(false, status, $"ASIC returned {status} ({response.ReasonPhrase}) {via}."),
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AsicConnectionTestResult(false, null, $"Timed out reaching ASIC {via}.");
        }
        catch (Exception ex)
        {
            return new AsicConnectionTestResult(false, null, $"Could not reach ASIC {via}: {ex.Message}");
        }
    }
}
