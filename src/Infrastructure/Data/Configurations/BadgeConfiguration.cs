using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.Code).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.Condition).HasMaxLength(500);
        builder.Property(b => b.IconUrl).HasMaxLength(2000);
    }
}
