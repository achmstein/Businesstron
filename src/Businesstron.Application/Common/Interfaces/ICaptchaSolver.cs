namespace Businesstron.Application.Common.Interfaces;

/// <summary>
/// Outcome of a CAPTCHA solve: a token on success, or a human-readable reason on
/// failure (e.g. zero balance, bad key, timeout) so callers can surface *why*.
/// </summary>
public sealed record CaptchaSolveResult(string? Token, string? Error)
{
    public bool Succeeded => !string.IsNullOrEmpty(Token);

    public static CaptchaSolveResult Ok(string token) => new(token, null);

    public static CaptchaSolveResult Fail(string error) => new(null, error);
}

/// <summary>
/// Solves a reCAPTCHA challenge, returning the g-recaptcha-response token.
/// Abstracts the concrete provider (2Captcha) away from the ASIC client so it can
/// be swapped or mocked. On failure returns a result with a populated Error and no
/// token (no key configured, provider error, or timeout).
/// </summary>
public interface ICaptchaSolver
{
    bool IsConfigured { get; }

    Task<CaptchaSolveResult> SolveReCaptchaAsync(
        string siteKey, string pageUrl, string? action, bool invisible, CancellationToken cancellationToken);
}
