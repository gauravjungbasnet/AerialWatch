using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

/// <summary>A confirmed pig's position on the other camera's twin photo (D-040). One row per detection.</summary>
public sealed class TwinMarkConfiguration : IEntityTypeConfiguration<TwinMark>
{
    public void Configure(EntityTypeBuilder<TwinMark> b)
    {
        b.ToTable("TwinMarks", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.ReviewedBy).HasMaxLength(AnimalDetection.ReviewedByMaxLength);

        // Deleting a box (or its photo or survey) deletes its mark. TwinImageId has no foreign key on purpose:
        // a second cascade path from SurveyImages isn't allowed by SQL Server, and the twin goes with its survey anyway.
        b.HasOne(x => x.Detection).WithMany().HasForeignKey(x => x.DetectionId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.DetectionId).IsUnique(); // one mark per box; twin sightings without a box are allowed (filtered)
        b.HasIndex(x => x.CountedImageId);
        b.HasIndex(x => x.TwinImageId);
    }
}
