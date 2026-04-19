using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.FcmToken).IsUnique();

        builder.Property(d => d.UserId).IsRequired().HasMaxLength(450);
        builder.Property(d => d.FcmToken).IsRequired().HasMaxLength(500);
    }
}
