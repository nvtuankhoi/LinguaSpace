using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class UserXpConfiguration : IEntityTypeConfiguration<UserXp>
{
    public void Configure(EntityTypeBuilder<UserXp> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
    }
}
