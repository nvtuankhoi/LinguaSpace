using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        // Composite index for the primary list/filter query (includes RoomType for type-based filtering)
        builder.HasIndex(r => new { r.RoomType, r.Status, r.LanguageCode })
            .HasDatabaseName("IX_Rooms_Type_Status_LanguageCode");
        builder.HasIndex(r => r.HostId);

        builder.Property(r => r.Title).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(r => r.HostId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.LiveKitRoomName).HasMaxLength(200);

        builder.HasMany(r => r.Participants)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Messages)
            .WithOne(m => m.Room)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
