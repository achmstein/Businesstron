namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// Runs the optional web stage for a search run: for each suitable record whose
/// renewal is within the lead window, reverse-whois the holder's ABN to find
/// domains, then look each up on auda until an email is found, then (when
/// configured) enrich contact details. Durable and resumable like the main
/// processor, so a redeploy mid-run picks up where it left off.
/// </summary>
public interface IWebEnrichmentService
{
    Task ProcessAsync(Guid searchRunId, CancellationToken cancellationToken);
}
