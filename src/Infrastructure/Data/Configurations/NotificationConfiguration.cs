using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });

        builder.Property(n => n.RecipientId).IsRequired().HasMaxLength(450);
        builder.Property(n => n.Payload).HasMaxLength(2000);
    }
}
