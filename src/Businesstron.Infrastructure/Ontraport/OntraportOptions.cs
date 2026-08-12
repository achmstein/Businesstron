namespace Businesstron.Infrastructure.Ontraport;

public class OntraportOptions
{
    public const string SectionName = "Ontraport";

    public string BaseUrl { get; set; } = "https://api.ontraport.com/1/";

    /// <summary>Ontraport API App ID (Api-Appid header). Seeded from configuration/environment; editable from the Settings UI.</summary>
    public string? AppId { get; set; }

    /// <summary>Ontraport API Key (Api-Key header). Seeded from configuration/environment; editable from the Settings UI.</summary>
    public string? ApiKey { get; set; }
}
