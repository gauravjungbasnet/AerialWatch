using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence.Configurations;

/// <summary>One row per saved coefficient set; the newest row (by CreatedAt) is in use.</summary>
public sealed class ModelCoefficientSetConfiguration : IEntityTypeConfiguration<ModelCoefficientSet>
{
    public void Configure(EntityTypeBuilder<ModelCoefficientSet> b)
    {
        b.ToTable("ModelCoefficientSets", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Version).HasMaxLength(ModelCoefficientSet.VersionMaxLength).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(ModelCoefficientSet.CreatedByMaxLength);
        b.HasIndex(x => x.CreatedAt);
    }
}
