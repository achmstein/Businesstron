namespace Businesstron.Domain.Entities;

/// <summary>
/// A single business-name result row. Carries the same fields the console app
/// wrote to CSV, plus the suitability and Ontraport-push state that drive the
/// automation pipeline.
/// </summary>
public class BusinessNameRecord : BaseEntity
{
    public Guid SearchRunId { get; set; }
    public SearchRun SearchRun { get; set; } = null!;

    // --- Fields carried over from the original CsvRecord ---
    public string? BusinessName { get; set; }
    public string? Status { get; set; }
    public string? RegistrationDate { get; set; }
    public string? RenewalDate { get; set; }
    public string? CancelledDate { get; set; }
    public string? CancellationUnderReview { get; set; }
    public string? AddressForServiceDocuments { get; set; }
    public string? PrincipalPlaceOfBusiness { get; set; }
    public string? HolderName { get; set; }
    public string? HolderType { get; set; }
    public string? HolderAbn { get; set; }
    public string? AbnStatus { get; set; }
    public string? Gst { get; set; }
    public bool HasMultipleBusinessNames { get; set; }

    // --- Automation state ---

    /// <summary>Whether this row has been enriched from ASIC/ABR yet (drives the live monitor).</summary>
    public EnrichmentStatus EnrichmentStatus { get; set; } = EnrichmentStatus.Enriched;

    /// <summary>Why enrichment failed for this record (e.g. CAPTCHA not solved), if it did.</summary>
    public string? EnrichmentError { get; set; }

    /// <summary>False when a keyword filter matched the business name (e.g. gov, legal).</summary>
    public bool IsSuitable { get; set; } = true;

    /// <summary>The keyword that caused the record to be flagged unsuitable, if any.</summary>
    public string? SuitabilityReason { get; set; }

    public OntraportPushStatus OntraportStatus { get; set; } = OntraportPushStatus.NotPushed;

    public string? OntraportContactId { get; set; }

    public string? OntraportError { get; set; }

    public DateTimeOffset? PushedAt { get; set; }

    // --- Web enrichment (reverse-whois -> auda email -> contact) ---

    /// <summary>Where this row sits in the optional web-enrichment stage.</summary>
    public WebEnrichmentStatus WebEnrichmentStatus { get; set; } = WebEnrichmentStatus.NotAttempted;

    /// <summary>Why web enrichment failed for this record, if it did.</summary>
    public string? WebEnrichmentError { get; set; }

    /// <summary>Domains found for the holder via reverse-whois, comma-separated (in the order tried).</summary>
    public string? Websites { get; set; }

    /// <summary>Primary contact email — the first email found across the holder's domains.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>All contact emails found via auda, semicolon-separated.</summary>
    public string? ContactEmails { get; set; }

    /// <summary>Contact phone number, populated later by the contact-enrichment step.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Social-media links, populated later by the contact-enrichment step.</summary>
    public string? ContactSocials { get; set; }
}
