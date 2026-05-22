using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.HasIndex(b => new { b.BlockerId, b.BlockedId }).IsUnique();
        builder.Property(b => b.BlockerId).HasMaxLength(450).IsRequired();
        builder.Property(b => b.BlockedId).HasMaxLength(450).IsRequired();
    }
}
