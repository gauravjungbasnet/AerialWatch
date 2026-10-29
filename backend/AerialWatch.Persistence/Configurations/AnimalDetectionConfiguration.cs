using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence.Configurations;

public sealed class AnimalDetectionConfiguration : IEntityTypeConfiguration<AnimalDetection>
{
    public void Configure(EntityTypeBuilder<AnimalDetection> b)
    {
        b.ToTable("Detections", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Label).HasMaxLength(40).IsRequired();
        b.Property(x => x.ModelVersion).HasMaxLength(60);
        b.Property(x => x.Source).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        b.HasIndex(x => new { x.ImageId, x.Status });
    }
}
