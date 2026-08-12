using Businesstron.Application.FilterKeywords.Commands.CreateFilterKeyword;
using Businesstron.Application.FilterKeywords.Commands.DeleteFilterKeyword;
using Businesstron.Application.FilterKeywords.Commands.UpdateFilterKeyword;
using Businesstron.Application.FilterKeywords.Queries.GetFilterKeywords;
using MediatR;

namespace Businesstron.Web.Endpoints;

public static class FilterKeywordsEndpoints
{
    public record CreateKeywordRequest(string Word);
    public record UpdateKeywordRequest(string Word, bool IsActive);

    public static IEndpointRouteBuilder MapFilterKeywordsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/filter-keywords")
            .WithTags("Filter Keywords")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetFilterKeywordsQuery())));

        group.MapPost("/", async (CreateKeywordRequest request, ISender sender) =>
        {
            var id = await sender.Send(new CreateFilterKeywordCommand(request.Word));
            return Results.Ok(new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateKeywordRequest request, ISender sender) =>
        {
            await sender.Send(new UpdateFilterKeywordCommand(id, request.Word, request.IsActive));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteFilterKeywordCommand(id));
            return Results.NoContent();
        });

        return app;
    }
}
