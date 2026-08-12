using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Businesstron.Infrastructure.Data.Configurations;

public class OntraportConfigurationConfiguration : IEntityTypeConfiguration<OntraportConfiguration>
{
    public void Configure(EntityTypeBuilder<OntraportConfiguration> builder)
    {
        // Single-row table; no special mapping required.
    }
}
