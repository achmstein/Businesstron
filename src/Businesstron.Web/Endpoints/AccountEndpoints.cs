using System.Security.Claims;
using Businesstron.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Businesstron.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Account");

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }).RequireAuthorization();

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity.Name;
            return Results.Ok(new { email, name = email });
        }).RequireAuthorization();

        return app;
    }
}
