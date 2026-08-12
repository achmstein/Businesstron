using Hangfire.Dashboard;

namespace Businesstron.Web.Infrastructure;

/// <summary>Restricts the Hangfire dashboard to authenticated users.</summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}
