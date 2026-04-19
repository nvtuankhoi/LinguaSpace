using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class UserLanguageConfiguration : IEntityTypeConfiguration<UserLanguage>
{
    public void Configure(EntityTypeBuilder<UserLanguage> builder)
    {
        builder.HasIndex(ul => ul.UserProfileId);

        builder.Property(ul => ul.LanguageCode).IsRequired().HasMaxLength(10);

        builder.HasOne(ul => ul.UserProfile)
            .WithMany(p => p.Languages)
            .HasForeignKey(ul => ul.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
