using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class RoomParticipantConfiguration : IEntityTypeConfiguration<RoomParticipant>
{
    public void Configure(EntityTypeBuilder<RoomParticipant> builder)
    {
        // Unique: a user can only be in a room once
        builder.HasIndex(p => new { p.RoomId, p.UserId })
            .HasDatabaseName("IX_RoomParticipants_RoomId_UserId")
            .IsUnique();

        builder.Property(p => p.UserId).IsRequired().HasMaxLength(450);
    }
}
