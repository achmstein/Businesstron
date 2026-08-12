namespace Businesstron.Infrastructure.ExternalClients.Asic;

public class AsicOptions
{
    public const string SectionName = "Asic";

    public string BaseUrl { get; set; } = "https://connectonline.asic.gov.au/";

    /// <summary>ASIC's public reCAPTCHA site key (not a secret; belongs to their page).</summary>
    public string RecaptchaSiteKey { get; set; } = "6LdfxBoUAAAAAO7ItWGgMWT32_h5T_TtD4F1MflL";

    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0";

    /// <summary>
    /// Per-request HTTP timeout for the multi-step ASIC scraper. Higher than the .NET
    /// default of 100s because the registry is occasionally slow under load; a request
    /// that still exceeds this fails just its own record, not the whole run.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// How many ASIC sessions enrich records in parallel. Each worker owns an isolated
    /// cookie session (and solves its own CAPTCHA when challenged). Raising this speeds
    /// up large runs, but too many concurrent sessions risks ASIC throttling the IP.
    /// </summary>
    public int MaxConcurrency { get; set; } = 8;

    /// <summary>
    /// Abort a run after this many consecutive enrichment failures.
    /// <para>
    /// When ASIC becomes unreachable — a Cloudflare block, an outage — every remaining
    /// item fails identically, and without a stop the run walks the whole work list
    /// anyway. One such run issued roughly 160,000 doomed requests in 95 minutes, which
    /// achieves nothing and makes a block more likely to stick. Only transport failures
    /// count; a legitimate "no match on ASIC" is a normal result and resets the counter.
    /// </para>
    /// </summary>
    public int ConsecutiveFailureLimit { get; set; } = 25;

    /// <summary>
    /// Advertise TLS 1.3 only when connecting to ASIC.
    /// <para>
    /// ASIC sits behind Cloudflare, which fingerprints the TLS handshake. A default .NET
    /// ClientHello offers TLS 1.2 alongside 1.3, which Cloudflare classifies as
    /// automation and answers 403 — for every path on the domain, before the request is
    /// even read. Restricting to TLS 1.3 matches what a browser negotiates: measured on
    /// the deployed host, default succeeded 1/10 and TLS 1.3 succeeded 10/10.
    /// </para>
    /// <para>Only turn this off if ASIC ever stops supporting TLS 1.3.</para>
    /// </summary>
    public bool ForceTls13 { get; set; } = true;

    /// <summary>
    /// Outbound proxy for ASIC requests — e.g. "http://gate.provider.com:7000" or
    /// "socks5://gate.provider.com:1080". Blank connects directly.
    /// <para>
    /// ASIC sits behind Cloudflare, which scores datacenter IPs on reputation and will
    /// block one outright once it looks like a scraper — at which point every request
    /// from that host returns 403 for the whole domain, indefinitely. Routing through a
    /// residential or mobile proxy is what keeps enrichment working from a cloud server.
    /// </para>
    /// </summary>
    public string? ProxyUrl { get; set; }

    /// <summary>
    /// Proxy username. Kept separate from <see cref="ProxyUrl"/> so credentials don't
    /// have to be URL-encoded into the address; credentials embedded in the address are
    /// still honoured when this is blank.
    /// </summary>
    public string? ProxyUsername { get; set; }

    /// <summary>Proxy password, paired with <see cref="ProxyUsername"/>.</summary>
    public string? ProxyPassword { get; set; }
}
