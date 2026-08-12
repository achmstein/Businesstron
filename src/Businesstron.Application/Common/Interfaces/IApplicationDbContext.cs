using Microsoft.EntityFrameworkCore;

namespace Businesstron.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<SearchRun> SearchRuns { get; }
    DbSet<BusinessNameRecord> BusinessNameRecords { get; }
    DbSet<FilterKeyword> FilterKeywords { get; }
    DbSet<OntraportConfiguration> OntraportConfigurations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
