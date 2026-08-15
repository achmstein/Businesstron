namespace Businesstron.Infrastructure.Identity;

public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Shared API key for machine clients (sent in the X-Api-Key header). Seeded from
    /// configuration/environment; empty or missing disables API-key authentication.
    /// </summary>
    public string? ApiKey { get; set; }
}
