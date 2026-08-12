using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Businesstron.Infrastructure.Data.Configurations;

public class BusinessNameRecordConfiguration : IEntityTypeConfiguration<BusinessNameRecord>
{
    public void Configure(EntityTypeBuilder<BusinessNameRecord> builder)
    {
        builder.Property(r => r.BusinessName).HasMaxLength(512);
        builder.Property(r => r.HolderName).HasMaxLength(512);
        builder.Property(r => r.HolderAbn).HasMaxLength(64);
        builder.Property(r => r.SuitabilityReason).HasMaxLength(128);
        builder.Property(r => r.EnrichmentError).HasMaxLength(1000);
        builder.Property(r => r.OntraportContactId).HasMaxLength(64);
        builder.Property(r => r.OntraportError).HasMaxLength(1000);

        builder.HasIndex(r => r.SearchRunId);
        builder.HasIndex(r => r.IsSuitable);
    }
}
