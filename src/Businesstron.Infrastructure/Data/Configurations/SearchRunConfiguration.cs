using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Businesstron.Infrastructure.Data.Configurations;

public class SearchRunConfiguration : IEntityTypeConfiguration<SearchRun>
{
    public void Configure(EntityTypeBuilder<SearchRun> builder)
    {
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.Error).HasMaxLength(2000);
        builder.Property(r => r.Tags).HasMaxLength(512);

        builder.HasMany(r => r.Records)
            .WithOne(x => x.SearchRun)
            .HasForeignKey(x => x.SearchRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.Created);
    }
}
