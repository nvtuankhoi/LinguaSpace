using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // DESC for cursor-based pagination (newest first)
        builder.HasIndex(m => new { m.RoomId, m.SentAt })
            .HasDatabaseName("IX_Messages_RoomId_SentAt")
            .IsDescending(false, true);

        builder.Property(m => m.SenderId).IsRequired().HasMaxLength(450);
        builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);
    }
}
