using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.Identity;

/// <summary>
/// Header-based API-key authentication for machine clients (the Mastertron MCP gateway).
/// Validates the <c>X-Api-Key</c> header against <c>Security:ApiKey</c>. When no key is
/// configured or the header is absent it returns NoResult so cookie authentication
/// handles the request exactly as before; only a present-but-wrong key fails.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<SecurityOptions> security)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // IOptionsMonitor (not IOptions) so a key added via the overrides file or a
        // config reload takes effect without a restart, like the other credentials.
        var configuredKey = security.CurrentValue.ApiKey;

        if (string.IsNullOrEmpty(configuredKey) ||
            !Request.Headers.TryGetValue(HeaderName, out var providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedKey.ToString()),
                Encoding.UTF8.GetBytes(configuredKey)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "mastertron"),
                new Claim(ClaimTypes.NameIdentifier, "mastertron"),
            ],
            Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
