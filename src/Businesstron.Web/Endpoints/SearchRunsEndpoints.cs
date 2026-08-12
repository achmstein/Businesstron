using Businesstron.Application.SearchRuns.Commands.CancelSearchRun;
using Businesstron.Application.SearchRuns.Commands.CreateSearchRun;
using Businesstron.Application.SearchRuns.Commands.DeleteSearchRun;
using Businesstron.Application.SearchRuns.Commands.PushRunToOntraport;
using Businesstron.Application.SearchRuns.Commands.RetryFailedEnrichment;
using Businesstron.Application.SearchRuns.Commands.UpdateSearchRunTags;
using Businesstron.Application.SearchRuns.Queries.ExportSearchRunCsv;
using Businesstron.Application.SearchRuns.Queries.GetSearchRun;
using Businesstron.Application.SearchRuns.Queries.GetSearchRuns;
using MediatR;

namespace Businesstron.Web.Endpoints;

public static class SearchRunsEndpoints
{
    public record UpdateTagsRequest(List<string> Tags);

    public static IEndpointRouteBuilder MapSearchRunsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search-runs")
            .WithTags("Search Runs")
            .RequireAuthorization();

        group.MapPost("/", async (CreateSearchRunCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Ok(new { id });
        });

        group.MapGet("/", async (ISender sender, int? page, int? pageSize) =>
            Results.Ok(await sender.Send(new GetSearchRunsQuery(page ?? 1, pageSize ?? 20))));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, int? page, int? pageSize, string? filter) =>
            Results.Ok(await sender.Send(new GetSearchRunQuery(id, page ?? 1, pageSize ?? 100, filter))));

        group.MapPut("/{id:guid}/tags", async (Guid id, UpdateTagsRequest request, ISender sender) =>
        {
            await sender.Send(new UpdateSearchRunTagsCommand(id, request.Tags));
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/export", async (Guid id, bool? onlySuitable, ISender sender, HttpContext http) =>
        {
            var file = await sender.Send(new ExportSearchRunCsvQuery(id, onlySuitable ?? false));
            return Results.Stream(
                stream => file.WriteTo(stream, http.RequestAborted),
                "text/csv",
                file.FileName);
        });

        group.MapPost("/{id:guid}/push", async (Guid id, ISender sender) =>
        {
            await sender.Send(new PushRunToOntraportCommand(id));
            return Results.Accepted();
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender) =>
        {
            await sender.Send(new CancelSearchRunCommand(id));
            return Results.Accepted();
        });

        group.MapPost("/{id:guid}/retry", async (Guid id, ISender sender) =>
        {
            await sender.Send(new RetryFailedEnrichmentCommand(id));
            return Results.Accepted();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteSearchRunCommand(id));
            return Results.NoContent();
        });

        return app;
    }
}
