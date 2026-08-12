namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// Outcome of an auda WHOIS lookup for one domain: the emails scraped from the
/// registrant record, or a reason the lookup could not complete.
/// </summary>
/// <param name="Succeeded">True when the WHOIS record was retrieved (even if it held no email).</param>
/// <param name="Emails">Distinct contact emails found in the record (noreply/abuse filtered out).</param>
/// <param name="Error">Human-readable reason on failure (CAPTCHA, transport), else null.</param>
public sealed record AudaLookupResult(bool Succeeded, IReadOnlyList<string> Emails, string? Error)
{
    public static AudaLookupResult Ok(IReadOnlyList<string> emails) => new(true, emails, null);
    public static AudaLookupResult Fail(string error) => new(false, [], error);
}

/// <summary>
/// Looks up a single .au domain on <c>whois.auda.org.au</c> — solving the page's
/// reCAPTCHA via the shared <see cref="ICaptchaSolver"/> — and returns the contact
/// emails in the registrant record. Ports the console AudaTron tool.
/// </summary>
public interface IAudaClient
{
    Task<AudaLookupResult> LookupAsync(string domain, CancellationToken cancellationToken);
}
