namespace Businesstron.Domain.Enums;

/// <summary>
/// Progress of the optional web-enrichment stage for one record: reverse-whois to
/// find the holder's domains, then an auda WHOIS lookup to pull a contact email
/// (and, later, a contact-enrichment step for phone/socials).
/// </summary>
public enum WebEnrichmentStatus
{
    /// <summary>Web enrichment was not requested for this run, or not reached yet.</summary>
    NotAttempted = 0,

    /// <summary>Queued for web enrichment (eligible: suitable + renewal within the window).</summary>
    Pending = 1,

    /// <summary>Excluded from web enrichment (unsuitable, or renewal outside the window).</summary>
    Skipped = 2,

    /// <summary>Reverse-whois returned no domains for the holder.</summary>
    NoWebsite = 3,

    /// <summary>Domains were found but no auda lookup yielded an email.</summary>
    NoEmail = 4,

    /// <summary>At least a website (and usually an email) was found.</summary>
    Enriched = 5,

    /// <summary>A transport/provider error stopped enrichment for this record.</summary>
    Failed = 6
}
