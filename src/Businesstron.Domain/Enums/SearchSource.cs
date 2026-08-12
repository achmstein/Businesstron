namespace Businesstron.Domain.Enums;

public enum SearchSource
{
    /// <summary>Pull newly registered business names from data.gov.au for a date range.</summary>
    DataGov = 0,

    /// <summary>Process an explicit list of ABNs supplied by the user.</summary>
    AbnList = 1
}
