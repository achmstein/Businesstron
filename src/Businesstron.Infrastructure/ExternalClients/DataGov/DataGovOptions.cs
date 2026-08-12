namespace Businesstron.Infrastructure.ExternalClients.DataGov;

public class DataGovOptions
{
    public const string SectionName = "DataGov";

    public string BaseUrl { get; set; } = "https://data.gov.au/data/api/3/action/";

    /// <summary>data.gov.au datastore resource id for the ASIC business-names dataset.</summary>
    public string ResourceId { get; set; } = "55ad4b1c-5eeb-44ea-8b29-d410da431be3";
}
