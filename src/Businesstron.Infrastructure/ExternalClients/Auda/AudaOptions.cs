namespace Businesstron.Infrastructure.ExternalClients.Auda;

public class AudaOptions
{
    public const string SectionName = "Auda";

    public string BaseUrl { get; set; } = "https://whois.auda.org.au/";

    /// <summary>
    /// auda's public reCAPTCHA Enterprise *checkbox* site key (belongs to their page, not
    /// a secret). This is the key behind the "I'm not a robot" fallback auda accepts when
    /// its frictionless score check is not satisfied — the path we drive, since solving
    /// services can clear a checkbox but not a score gate. Distinct from the score-check
    /// key the landing page loads.
    /// </summary>
    public string RecaptchaSiteKey { get; set; } = "6Ld3MSMqAAAAAIU_qrBNaNvuLzZf5EaSDwhEdJYA";

    /// <summary>The reCAPTCHA Enterprise action auda's checkbox is rendered with.</summary>
    public string RecaptchaAction { get; set; } = "WhoisWebQuery";

    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0";

    /// <summary>Per-request HTTP timeout for the auda scrape.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>How many times to re-solve/re-submit when auda returns the form (a CAPTCHA rejection).</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Politeness pause after each successful lookup, to avoid tripping auda's rate limits.</summary>
    public int DelayBetweenRequestsMs { get; set; } = 4000;
}
