namespace Businesstron.Infrastructure.ExternalClients.WhoisXml;

public class WhoisXmlOptions
{
    public const string SectionName = "WhoisXml";

    public string BaseUrl { get; set; } = "https://reverse-whois.whoisxmlapi.com/api/v2";

    /// <summary>WhoisXML API key (Domains Research Suite). Seeded from config; editable in Settings.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// "purchase" returns the actual <c>domainsList</c> (consumes DRS credits); "preview"
    /// returns only a count. The pipeline needs the list, so purchase is the default.
    /// </summary>
    public string Mode { get; set; } = "purchase";

    /// <summary>"current" matches present-day WHOIS records; "historic" also searches past ones.</summary>
    public string SearchType { get; set; } = "current";

    public int TimeoutSeconds { get; set; } = 60;
}
