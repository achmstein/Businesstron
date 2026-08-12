using Microsoft.AspNetCore.Identity;

namespace Businesstron.Infrastructure.Identity;

/// <summary>Identity requires an IEmailSender; this internal admin tool doesn't send account email.</summary>
public sealed class NoOpEmailSender : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => Task.CompletedTask;
}
