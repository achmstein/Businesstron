using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Businesstron.Infrastructure.Data.Configurations;

public class FilterKeywordConfiguration : IEntityTypeConfiguration<FilterKeyword>
{
    public void Configure(EntityTypeBuilder<FilterKeyword> builder)
    {
        builder.Property(k => k.Word).HasMaxLength(100).IsRequired();
        builder.HasIndex(k => k.Word).IsUnique();
    }
}
