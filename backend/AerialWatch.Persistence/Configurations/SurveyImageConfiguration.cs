using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence.Configurations;

public sealed class SurveyImageConfiguration : IEntityTypeConfiguration<SurveyImage>
{
    public void Configure(EntityTypeBuilder<SurveyImage> b)
    {
        b.ToTable("SurveyImages", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        b.HasIndex(x => x.SurveyId);
        b.HasMany(x => x.Detections).WithOne(d => d.Image).HasForeignKey(d => d.ImageId).OnDelete(DeleteBehavior.Cascade);
    }
}
