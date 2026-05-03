using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // Enforce uniqueness: only one conversation between any two users
        builder.HasIndex(c => new { c.User1Id, c.User2Id })
            .HasDatabaseName("IX_Conversations_User1Id_User2Id")
            .IsUnique();
        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_Conversations_LastMessageAt")
            .IsDescending();

        builder.Property(c => c.User1Id).IsRequired().HasMaxLength(450);
        builder.Property(c => c.User2Id).IsRequired().HasMaxLength(450);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
