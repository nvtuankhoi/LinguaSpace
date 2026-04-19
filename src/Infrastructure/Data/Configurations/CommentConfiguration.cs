using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasIndex(c => c.PostId);

        builder.Property(c => c.AuthorId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);

        // Self-referencing for replies; no cascade to avoid accidental deletion of thread
        builder.HasOne(c => c.ParentComment)
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
