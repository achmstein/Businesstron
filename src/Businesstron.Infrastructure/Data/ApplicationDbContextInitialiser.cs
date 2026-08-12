using Businesstron.Domain.Services;
using Businesstron.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Businesstron.Infrastructure.Data;

/// <summary>Applies migrations and seeds the admin user, default filter keywords, and Ontraport config row.</summary>
public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
{
    public async Task InitialiseAsync()
    {
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedAdminUserAsync();
            await SeedFilterKeywordsAsync();
            await SeedOntraportConfigurationAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var email = configuration["SeedAdmin:Email"] ?? "admin@businesstron.com";
        var password = configuration["SeedAdmin:Password"] ?? "Administrator1!";

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);

        if (result.Succeeded)
        {
            logger.LogInformation("Seeded admin user {Email}.", email);
        }
        else
        {
            logger.LogWarning("Failed to seed admin user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedFilterKeywordsAsync()
    {
        if (await context.FilterKeywords.AnyAsync())
        {
            return;
        }

        context.FilterKeywords.AddRange(
            SuitabilityEvaluator.DefaultKeywords.Select(w => new FilterKeyword { Word = w, IsActive = true }));

        await context.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Seeded {Count} default filter keywords.", SuitabilityEvaluator.DefaultKeywords.Count);
    }

    private async Task SeedOntraportConfigurationAsync()
    {
        if (await context.OntraportConfigurations.AnyAsync())
        {
            return;
        }

        context.OntraportConfigurations.Add(new OntraportConfiguration { AutoPushEnabled = false });
        await context.SaveChangesAsync(CancellationToken.None);
    }
}

public static class InitialiserExtensions
{
    /// <summary>Runs migration + seeding at startup within a fresh scope.</summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}
