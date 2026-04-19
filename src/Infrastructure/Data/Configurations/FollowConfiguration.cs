using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        // A user can only follow another user once
        builder.HasIndex(f => new { f.FollowerId, f.FolloweeId }).IsUnique();
        builder.HasIndex(f => f.FolloweeId);

        builder.Property(f => f.FollowerId).IsRequired().HasMaxLength(450);
        builder.Property(f => f.FolloweeId).IsRequired().HasMaxLength(450);
    }
}
