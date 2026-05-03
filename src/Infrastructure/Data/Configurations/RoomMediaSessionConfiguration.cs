using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class RoomMediaSessionConfiguration : IEntityTypeConfiguration<RoomMediaSession>
{
    public void Configure(EntityTypeBuilder<RoomMediaSession> builder)
    {
        builder.HasIndex(s => new { s.RoomId, s.JoinedAt })
            .HasDatabaseName("IX_RoomMediaSessions_RoomId_JoinedAt");
        builder.HasIndex(s => new { s.UserId, s.JoinedAt })
            .HasDatabaseName("IX_RoomMediaSessions_UserId_JoinedAt");

        builder.Property(s => s.UserId).IsRequired().HasMaxLength(450);

        builder.HasOne(s => s.Room)
            .WithMany()
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
