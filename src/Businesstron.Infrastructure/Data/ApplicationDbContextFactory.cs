using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Businesstron.Infrastructure.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build the context without
/// the Aspire host. Uses a placeholder connection string unless one is supplied via
/// the ConnectionStrings__BusinesstronDb environment variable.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__BusinesstronDb")
            ?? "Host=localhost;Port=5432;Database=BusinesstronDb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
