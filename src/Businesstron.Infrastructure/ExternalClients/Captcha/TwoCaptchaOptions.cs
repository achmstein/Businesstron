namespace Businesstron.Infrastructure.ExternalClients.Captcha;

public class TwoCaptchaOptions
{
    public const string SectionName = "TwoCaptcha";

    /// <summary>2Captcha account API key. Seeded from configuration/environment; editable from the Settings UI.</summary>
    public string? ApiKey { get; set; }

    public int DefaultTimeoutSeconds { get; set; } = 120;
    public int RecaptchaTimeoutSeconds { get; set; } = 600;
    public int PollingIntervalSeconds { get; set; } = 10;
}
