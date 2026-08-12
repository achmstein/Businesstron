namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// Finds the domains registered against a search term (an ABN/ACN, which appears in
/// the registrant fields of .au WHOIS records) via a reverse-WHOIS provider.
/// Abstracts the concrete provider (WhoisXML API) from the pipeline.
/// </summary>
public interface IReverseWhoisClient
{
    /// <summary>True when an API key is configured, so the pipeline can fail fast with a clear reason.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Returns the domains matching <paramref name="searchTerm"/>, or an empty list
    /// when none match. Throws only on transport/quota errors so the caller can mark
    /// the record failed (as opposed to "no website found").
    /// </summary>
    Task<IReadOnlyList<string>> FindDomainsAsync(string searchTerm, CancellationToken cancellationToken);
}
