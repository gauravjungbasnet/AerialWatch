using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

/// <summary>Lookup table of the coefficients the research model uses. Ids are fixed (seeded), not generated.</summary>
public sealed class CoefficientTypeConfiguration : IEntityTypeConfiguration<CoefficientType>
{
    public void Configure(EntityTypeBuilder<CoefficientType> b)
    {
        b.ToTable("CoefficientTypes", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(CoefficientType.CodeMaxLength).IsRequired();
        b.Property(x => x.Camera).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Model).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Term).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Label).HasMaxLength(CoefficientType.LabelMaxLength).IsRequired();
        b.Property(x => x.Description).HasMaxLength(CoefficientType.DescriptionMaxLength).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.Camera, x.Model, x.Term }).IsUnique();
    }
}
