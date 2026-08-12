namespace Businesstron.Infrastructure.ExternalClients.Auda;

public class AudaOptions
{
    public const string SectionName = "Auda";

    public string BaseUrl { get; set; } = "https://whois.auda.org.au/";

    /// <summary>auda's public reCAPTCHA v2 site key (belongs to their page, not a secret).</summary>
    public string RecaptchaSiteKey { get; set; } = "6Ld3MSMqAAAAAIU_qrBNaNvuLzZf5EaSDwhEdJYA";

    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0";

    /// <summary>Per-request HTTP timeout for the auda scrape.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>How many times to re-solve/re-submit when auda returns the form (a CAPTCHA rejection).</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Politeness pause after each successful lookup, to avoid tripping auda's rate limits.</summary>
    public int DelayBetweenRequestsMs { get; set; } = 4000;
}
