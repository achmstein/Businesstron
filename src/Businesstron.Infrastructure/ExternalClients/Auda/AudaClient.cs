using System.Net;
using System.Text.RegularExpressions;
using Businesstron.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Businesstron.Infrastructure.ExternalClients.Auda;

/// <summary>
/// Looks a .au domain up on <c>whois.auda.org.au</c> and pulls the registrant contact
/// emails. Ports the console AudaTron tool, but shares Businesstron's
/// <see cref="ICaptchaSolver"/> (2Captcha) instead of newing its own, and reads the
/// live options so Settings edits apply without a restart.
/// <para>
/// Each lookup uses a fresh cookie jar so the page's antiforgery token and its cookie
/// stay paired even when several lookups run concurrently.
/// </para>
/// </summary>
public sealed partial class AudaClient(
    ICaptchaSolver captcha, IOptionsMonitor<AudaOptions> options, ILogger<AudaClient> logger)
    : IAudaClient
{
    public async Task<AudaLookupResult> LookupAsync(string domain, CancellationToken cancellationToken)
    {
        var opts = options.CurrentValue;

        if (!captcha.IsConfigured)
        {
            return AudaLookupResult.Fail("No 2Captcha API key is configured. Set it in Settings → 2Captcha.");
        }

        using var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", opts.UserAgent);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");

        string? lastError = null;

        for (var attempt = 1; attempt <= Math.Max(1, opts.MaxAttempts); attempt++)
        {
            try
            {
                var html = await LookupOnceAsync(http, opts, domain, cancellationToken);

                if (IsResultPage(html))
                {
                    // Politeness pause after a successful lookup so a run of website-having
                    // records doesn't spike auda's rate limiter (which returns 403 and, with
                    // enough of them, trips the web-stage failure breaker).
                    if (opts.DelayBetweenRequestsMs > 0)
                    {
                        await Task.Delay(opts.DelayBetweenRequestsMs, cancellationToken);
                    }

                    return AudaLookupResult.Ok(ExtractEmails(html));
                }

                // The query form came back instead of results — the CAPTCHA was rejected.
                // Retry with a fresh solve.
                lastError = "auda returned the query form (CAPTCHA rejected).";
                logger.LogDebug("auda lookup for {Domain} attempt {Attempt} rejected; retrying.", domain, attempt);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                logger.LogDebug(ex, "auda lookup for {Domain} attempt {Attempt} failed.", domain, attempt);
            }

            if (attempt < opts.MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }

        return AudaLookupResult.Fail(lastError ?? "auda lookup failed.");
    }

    private async Task<string> LookupOnceAsync(HttpClient http, AudaOptions opts, string domain, CancellationToken ct)
    {
        // 1) GET the page to obtain the antiforgery token (and set the paired cookie).
        var page = await http.GetStringAsync(opts.BaseUrl, ct);
        var token = ExtractToken(page)
            ?? throw new InvalidOperationException("Could not extract auda antiforgery token.");

        // 2) Solve auda's reCAPTCHA Enterprise CHECKBOX. As of Aug 2026 the page's initial
        //    landing captcha is a frictionless Enterprise *score* check (CaptchaType=Score)
        //    that solving services can't clear — but auda still accepts a checkbox-verified
        //    submission (its own fallback for a low score), which 2Captcha solves reliably.
        //    So we always submit the checkbox flavour rather than the score one.
        var solve = await captcha.SolveReCaptchaAsync(
            opts.RecaptchaSiteKey, opts.BaseUrl,
            action: opts.RecaptchaAction, invisible: false, enterprise: true, ct);
        if (!solve.Succeeded)
        {
            throw new InvalidOperationException(solve.Error ?? "CAPTCHA solve failed.");
        }

        // 3) Submit the WHOIS query form.
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Query"] = domain,
            ["QueryType"] = "Domain",
            ["CaptchaToken"] = solve.Token!,
            ["CaptchaType"] = "Checkbox",
            ["__RequestVerificationToken"] = token
        });

        using var response = await http.PostAsync(opts.BaseUrl, form, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static string? ExtractToken(string html)
    {
        var match = TokenRegex().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// True when auda returned the WHOIS results page (as opposed to bouncing back the
    /// query form on a CAPTCHA rejection). The results page still embeds the search form,
    /// so we key off the results header/content rather than the absence of the form.
    /// </summary>
    private static bool IsResultPage(string html) =>
        html.Contains("WHOIS Search Results", StringComparison.OrdinalIgnoreCase) ||
        html.Contains("whoisHeader", StringComparison.OrdinalIgnoreCase) ||
        html.Contains("whoisContent", StringComparison.OrdinalIgnoreCase);

    /// <summary>Distinct contact emails in the WHOIS text, minus the noreply/abuse boilerplate.</summary>
    private static IReadOnlyList<string> ExtractEmails(string text)
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in EmailRegex().Matches(text))
        {
            var email = m.Value.ToLowerInvariant().Trim();
            if (!email.StartsWith("noreply@", StringComparison.OrdinalIgnoreCase) &&
                !email.StartsWith("abuse@", StringComparison.OrdinalIgnoreCase))
            {
                emails.Add(email);
            }
        }

        return emails.ToList();
    }

    [GeneratedRegex(@"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex EmailRegex();
}
