using Businesstron.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.ExternalClients.Captcha;

/// <summary>Solves reCAPTCHA via the 2Captcha service, isolating the concrete library.</summary>
public sealed class TwoCaptchaSolver(IOptionsMonitor<TwoCaptchaOptions> options, ILogger<TwoCaptchaSolver> logger)
    : ICaptchaSolver
{
    // CurrentValue (not a captured snapshot) so API-key edits made from the Settings
    // UI are picked up on the next solve without restarting this singleton.
    private TwoCaptchaOptions Options => options.CurrentValue;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Options.ApiKey);

    public async Task<CaptchaSolveResult> SolveReCaptchaAsync(
        string siteKey, string pageUrl, string? action, bool invisible, CancellationToken cancellationToken)
    {
        var opts = Options;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning("2Captcha API key not configured; cannot solve reCAPTCHA.");
            return CaptchaSolveResult.Fail("No 2Captcha API key is configured. Set it in Settings → 2Captcha.");
        }

        try
        {
            var solver = new TwoCaptcha.TwoCaptcha(opts.ApiKey)
            {
                DefaultTimeout = opts.DefaultTimeoutSeconds,
                RecaptchaTimeout = opts.RecaptchaTimeoutSeconds,
                PollingInterval = opts.PollingIntervalSeconds
            };

            var captcha = new TwoCaptcha.Captcha.ReCaptcha();
            captcha.SetSiteKey(siteKey);
            captcha.SetUrl(pageUrl);
            captcha.SetInvisible(invisible);
            captcha.SetEnterprise(false);
            if (!string.IsNullOrEmpty(action))
            {
                captcha.SetAction(action);
            }

            await solver.Solve(captcha);

            return string.IsNullOrEmpty(captcha.Code)
                ? CaptchaSolveResult.Fail("2Captcha returned an empty token.")
                : CaptchaSolveResult.Ok(captcha.Code);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "2Captcha solve failed for {PageUrl}.", pageUrl);
            return CaptchaSolveResult.Fail(Describe(ex));
        }
    }

    /// <summary>Maps a 2Captcha library exception to a short, actionable reason.</summary>
    private static string Describe(Exception ex)
    {
        var raw = ex.Message?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return "2Captcha returned no result or timed out.";
        }

        if (raw.Contains("ZERO_BALANCE", StringComparison.OrdinalIgnoreCase))
        {
            return "2Captcha account balance is zero — top up the account to keep solving CAPTCHAs.";
        }

        if (raw.Contains("WRONG_USER_KEY", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("KEY_DOES_NOT_EXIST", StringComparison.OrdinalIgnoreCase))
        {
            return "2Captcha rejected the API key (wrong or nonexistent). Check the key in Settings → 2Captcha.";
        }

        if (raw.Contains("UNSOLVABLE", StringComparison.OrdinalIgnoreCase))
        {
            return "2Captcha could not solve the CAPTCHA (ERROR_CAPTCHA_UNSOLVABLE); it may succeed on a re-run.";
        }

        if (raw.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "2Captcha timed out before returning a token.";
        }

        return $"2Captcha error: {raw}";
    }
}
