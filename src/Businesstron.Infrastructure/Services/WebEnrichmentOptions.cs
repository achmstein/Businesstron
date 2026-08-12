namespace Businesstron.Infrastructure.Services;

public class WebEnrichmentOptions
{
    public const string SectionName = "WebEnrichment";

    /// <summary>Only chase records whose business name renews within this many months.</summary>
    public int RenewalWindowMonths { get; set; } = 12;

    /// <summary>
    /// How many records are web-enriched in parallel. Kept low by default: each record
    /// triggers a CAPTCHA-gated auda scrape, and too much parallelism risks auda/2Captcha
    /// throttling. Hard-capped at <see cref="MaxConcurrencyLimit"/>.
    /// </summary>
    public int MaxConcurrency { get; set; } = 3;

    public const int MaxConcurrencyLimit = 8;

    /// <summary>
    /// Abort after this many consecutive record failures — when the WhoisXML quota is
    /// exhausted or auda is blocking, every remaining record fails identically and
    /// walking the rest just burns time and credits.
    /// </summary>
    public int ConsecutiveFailureLimit { get; set; } = 15;
}
