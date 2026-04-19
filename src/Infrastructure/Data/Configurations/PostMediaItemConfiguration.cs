using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class PostMediaItemConfiguration : IEntityTypeConfiguration<PostMediaItem>
{
    public void Configure(EntityTypeBuilder<PostMediaItem> builder)
    {
        builder.Property(m => m.Url).IsRequired().HasMaxLength(2000);
    }
}
