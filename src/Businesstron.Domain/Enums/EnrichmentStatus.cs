namespace Businesstron.Domain.Enums;

public enum EnrichmentStatus
{
    /// <summary>Harvested from the source (e.g. data.gov.au) but not yet enriched via ASIC/ABR.</summary>
    Pending = 0,

    /// <summary>ASIC/ABR lookup complete.</summary>
    Enriched = 1,

    /// <summary>ASIC/ABR lookup failed for this item.</summary>
    Failed = 2
}
