using System.Security.Claims;
using Businesstron.Application.Common.Interfaces;

namespace Businesstron.Web.Services;

/// <summary>Surfaces the authenticated user to the Application layer from the HTTP context.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : IUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? Id => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.Identity?.Name;
}
