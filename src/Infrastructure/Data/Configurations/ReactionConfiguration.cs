using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        // One reaction type per user per target
        builder.HasIndex(r => new { r.UserId, r.TargetId, r.TargetType }).IsUnique();

        builder.Property(r => r.UserId).IsRequired().HasMaxLength(450);
    }
}
