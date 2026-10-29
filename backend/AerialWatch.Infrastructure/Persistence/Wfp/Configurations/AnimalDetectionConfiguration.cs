using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

public sealed class AnimalDetectionConfiguration : IEntityTypeConfiguration<AnimalDetection>
{
    public void Configure(EntityTypeBuilder<AnimalDetection> b)
    {
        b.ToTable("Detections", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Label).HasMaxLength(AnimalDetection.LabelMaxLength).IsRequired();
        b.Property(x => x.ModelVersion).HasMaxLength(AnimalDetection.ModelVersionMaxLength);
        b.Property(x => x.Source).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.ReviewedBy).HasMaxLength(AnimalDetection.ReviewedByMaxLength); // D-034 audit
        b.HasIndex(x => new { x.ImageId, x.Status });
    }
}
