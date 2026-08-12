using Businesstron.Application.Common.Interfaces;

namespace Businesstron.Infrastructure.ExternalClients.Contact;

/// <summary>
/// Placeholder contact enricher. Reports itself unconfigured so the web-enrichment
/// pipeline skips the contact step entirely. Swap this registration for a Google
/// Places / My Business or AI-website-scrape implementation to light the step up —
/// no pipeline changes required.
/// </summary>
public sealed class NoOpContactEnricher : IContactEnricher
{
    public bool IsConfigured => false;

    public Task<ContactEnrichmentResult> EnrichAsync(ContactEnrichmentInput input, CancellationToken cancellationToken) =>
        Task.FromResult(ContactEnrichmentResult.Empty);
}
