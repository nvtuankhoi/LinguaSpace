using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class XpTransactionConfiguration : IEntityTypeConfiguration<XpTransaction>
{
    public void Configure(EntityTypeBuilder<XpTransaction> builder)
    {
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.EarnedAt });
    }
}
