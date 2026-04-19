using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinguaSpace.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.ReporterId);

        builder.Property(r => r.ReporterId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.TargetId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.TargetType).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.ResolvedBy).HasMaxLength(450);
    }
}
