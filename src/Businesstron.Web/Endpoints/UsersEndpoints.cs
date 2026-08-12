using System.Security.Claims;
using Businesstron.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Web.Endpoints;

/// <summary>
/// Admin user management (list / create / reset password / delete). The app has no role
/// model — every authenticated user is an admin — so these mirror the rest of the API and
/// only require an authenticated session.
/// </summary>
public static class UsersEndpoints
{
    public record CreateUserRequest(string Email, string Password);
    public record ResetPasswordRequest(string NewPassword);

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/", async (UserManager<ApplicationUser> userManager) =>
        {
            var users = await userManager.Users
                .OrderBy(u => u.Email)
                .Select(u => new UserDto(
                    u.Id,
                    u.Email,
                    u.EmailConfirmed,
                    u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow))
                .ToListAsync();

            return Results.Ok(users);
        });

        group.MapPost("/", async (CreateUserRequest request, UserManager<ApplicationUser> userManager) =>
        {
            var email = request.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Problem("Email and password are required.");
            }

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                return Problem("A user with that email already exists.", StatusCodes.Status409Conflict);
            }

            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, request.Password);

            return result.Succeeded
                ? Results.Ok(new { id = user.Id })
                : Problem(Describe(result));
        });

        group.MapPost("/{id}/reset-password", async (string id, ResetPasswordRequest request, UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Problem("A new password is required.");
            }

            var user = await userManager.FindByIdAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

            return result.Succeeded ? Results.NoContent() : Problem(Describe(result));
        });

        group.MapDelete("/{id}", async (string id, UserManager<ApplicationUser> userManager, ClaimsPrincipal principal) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (user.Id == userManager.GetUserId(principal))
            {
                return Problem("You can't delete your own account.");
            }

            if (await userManager.Users.CountAsync() <= 1)
            {
                return Problem("Can't delete the last remaining user.");
            }

            var result = await userManager.DeleteAsync(user);
            return result.Succeeded ? Results.NoContent() : Problem(Describe(result));
        });

        return app;
    }

    private record UserDto(string Id, string? Email, bool EmailConfirmed, bool LockedOut);

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));

    private static IResult Problem(string detail, int statusCode = StatusCodes.Status400BadRequest) =>
        Results.Problem(detail: detail, statusCode: statusCode);
}
