using Businesstron.Application.Common.Interfaces;
using Businesstron.Application.Settings.Commands.UpdateOntraportConfiguration;
using Businesstron.Application.Settings.Queries.GetOntraportConfiguration;
using MediatR;

namespace Businesstron.Web.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        // Ontraport push mapping (tag / sequence / auto-push) — persisted in the DB.
        group.MapGet("/ontraport", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetOntraportConfigurationQuery())));

        group.MapPut("/ontraport", async (UpdateOntraportConfigurationCommand command, ISender sender) =>
        {
            await sender.Send(command);
            return Results.NoContent();
        });

        // Ontraport API credentials — persisted to the writable overrides file.
        group.MapGet("/ontraport-credentials", (ISettingsService settings) =>
            Results.Ok(settings.GetOntraportCredentials()));

        group.MapPut("/ontraport-credentials", async (OntraportCredentials body, ISettingsService settings, CancellationToken ct) =>
        {
            await settings.UpdateOntraportCredentialsAsync(body, ct);
            return Results.NoContent();
        });

        // 2Captcha API credentials — persisted to the writable overrides file.
        group.MapGet("/captcha", (ISettingsService settings) =>
            Results.Ok(settings.GetTwoCaptchaCredentials()));

        group.MapPut("/captcha", async (TwoCaptchaCredentials body, ISettingsService settings, CancellationToken ct) =>
        {
            await settings.UpdateTwoCaptchaCredentialsAsync(body, ct);
            return Results.NoContent();
        });

        // ASIC enrichment tuning and proxy routing — persisted to the writable overrides file.
        group.MapGet("/asic", (ISettingsService settings) =>
        {
            var asic = settings.GetAsicSettings();
            return Results.Ok(new
            {
                asic.MaxConcurrency,
                MaxConcurrencyLimit = AsicSettings.MaxConcurrencyLimit,
                asic.ForceTls13,
                asic.ProxyUrl,
                asic.ProxyUsername,
                asic.ProxyPassword,
            });
        });

        group.MapPut("/asic", async (AsicSettings body, ISettingsService settings, CancellationToken ct) =>
        {
            if (body.MaxConcurrency is < 1 or > AsicSettings.MaxConcurrencyLimit)
            {
                return Results.Problem(
                    $"Max concurrency must be between 1 and {AsicSettings.MaxConcurrencyLimit}.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Reject an unusable proxy here rather than letting every worker in the next
            // run throw on construction.
            if (settings.ValidateAsicSettings(body) is { } proxyError)
            {
                return Results.Problem(proxyError, statusCode: StatusCodes.Status400BadRequest);
            }

            await settings.UpdateAsicSettingsAsync(body, ct);
            return Results.NoContent();
        });

        // One-request probe against ASIC using the supplied settings, without saving them.
        group.MapPost("/asic/test", async (AsicSettings body, ISettingsService settings, CancellationToken ct) =>
            Results.Ok(await settings.TestAsicConnectionAsync(body, ct)));

        return app;
    }
}
