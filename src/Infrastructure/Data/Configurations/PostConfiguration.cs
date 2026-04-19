using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasIndex(p => p.AuthorId);
        builder.HasIndex(p => new { p.LanguageCode, p.Created });

        builder.Property(p => p.AuthorId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.Content).IsRequired().HasMaxLength(5000);
        builder.Property(p => p.LanguageCode).HasMaxLength(10);

        builder.HasMany(p => p.MediaItems)
            .WithOne(m => m.Post)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tags)
            .WithOne(t => t.Post)
            .HasForeignKey(t => t.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
