namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// What we already know about a lead going into the final contact step, gathered
/// from ASIC/ABR + reverse-whois + auda.
/// </summary>
public sealed record ContactEnrichmentInput(
    string? BusinessName,
    string? HolderName,
    string? Address,
    IReadOnlyList<string> Websites,
    string? KnownEmail);

/// <summary>
/// Extra contact details discovered for a lead. Any field may be null when not found.
/// </summary>
public sealed record ContactEnrichmentResult(string? Phone, string? ExtraEmail, string? Socials)
{
    public static readonly ContactEnrichmentResult Empty = new(null, null, null);
}

/// <summary>
/// Final step of the flow: given a lead's name, website and address, find a contact
/// phone number, an extra email, and social links — via Google Places / My Business
/// or an AI read of the website.
/// <para>
/// Deliberately a seam: the shipped implementation is a no-op
/// (<c>IsConfigured == false</c>) so the pipeline skips it cleanly until a concrete
/// provider is wired up.
/// </para>
/// </summary>
public interface IContactEnricher
{
    /// <summary>False for the no-op default, so the pipeline can skip the call entirely.</summary>
    bool IsConfigured { get; }

    Task<ContactEnrichmentResult> EnrichAsync(ContactEnrichmentInput input, CancellationToken cancellationToken);
}
