using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

public sealed class SurveyImageConfiguration : IEntityTypeConfiguration<SurveyImage>
{
    public void Configure(EntityTypeBuilder<SurveyImage> b)
    {
        b.ToTable("SurveyImages", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(SurveyImage.FileNameMaxLength).IsRequired();
        b.Property(x => x.StoragePath).HasMaxLength(SurveyImage.StoragePathMaxLength).IsRequired();
        b.Property(x => x.ThumbnailPath).HasMaxLength(SurveyImage.StoragePathMaxLength);
        b.Property(x => x.ContentType).HasMaxLength(SurveyImage.ContentTypeMaxLength).IsRequired();
        b.Property(x => x.ReviewedBy).HasMaxLength(AnimalDetection.ReviewedByMaxLength); // D-034 audit
        b.Property(x => x.Sha256).HasMaxLength(SurveyImage.Sha256Length).IsFixedLength().IsUnicode(false).IsRequired();

        // Thermal + colour twins (D-039): which camera took it, and the file-name key shared by a photo and its twin.
        b.Property(x => x.Camera).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.PairKey).HasMaxLength(SurveyImage.PairKeyMaxLength);
        b.HasIndex(x => new { x.SurveyId, x.PairKey });

        // One copy of a photo per survey (duplicate uploads are skipped); also serves "photos of survey X" queries.
        b.HasIndex(x => new { x.SurveyId, x.Sha256 }).IsUnique();
        b.HasMany(x => x.Detections).WithOne(d => d.Image).HasForeignKey(d => d.ImageId).OnDelete(DeleteBehavior.Cascade);
    }
}
