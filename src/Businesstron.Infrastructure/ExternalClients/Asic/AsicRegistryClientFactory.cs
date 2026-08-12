using Businesstron.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.ExternalClients.Asic;

/// <summary>Creates per-worker <see cref="AsicRegistryClient"/> instances (isolated cookie sessions).</summary>
public sealed class AsicRegistryClientFactory(IOptionsMonitor<AsicOptions> options, ICaptchaSolver captcha)
    : IAsicRegistryClientFactory
{
    // CurrentValue per call (not IOptions) so a Settings-page edit — proxy above all —
    // reaches the next run's clients without restarting the container.
    public IAsicRegistryClient Create() => new AsicRegistryClient(options.CurrentValue, captcha);
}
