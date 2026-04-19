using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class DirectMessageConfiguration : IEntityTypeConfiguration<DirectMessage>
{
    public void Configure(EntityTypeBuilder<DirectMessage> builder)
    {
        builder.HasIndex(m => new { m.ConversationId, m.SentAt });

        builder.Property(m => m.SenderId).IsRequired().HasMaxLength(450);
        builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);
    }
}
