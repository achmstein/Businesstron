namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// Creates independent ASIC registry sessions. Parallel enrichment workers each need
/// their own client because the scraper's cookies and ADF window state live per instance.
/// </summary>
public interface IAsicRegistryClientFactory
{
    /// <summary>Creates a client with a fresh, isolated session. Dispose it when the worker is done.</summary>
    IAsicRegistryClient Create();
}
