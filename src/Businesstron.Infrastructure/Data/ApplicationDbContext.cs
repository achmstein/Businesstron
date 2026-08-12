using System.Reflection;
using Businesstron.Application.Common.Interfaces;
using Businesstron.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Businesstron.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<SearchRun> SearchRuns => Set<SearchRun>();
    public DbSet<BusinessNameRecord> BusinessNameRecords => Set<BusinessNameRecord>();
    public DbSet<FilterKeyword> FilterKeywords => Set<FilterKeyword>();
    public DbSet<OntraportConfiguration> OntraportConfigurations => Set<OntraportConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
