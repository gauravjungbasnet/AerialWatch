using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

/// <summary>One row per saved coefficient set (header); the newest set (by CreatedAt) is in use. Values are in ModelCoefficientValues.</summary>
public sealed class ModelCoefficientSetConfiguration : IEntityTypeConfiguration<ModelCoefficientSet>
{
    public void Configure(EntityTypeBuilder<ModelCoefficientSet> b)
    {
        b.ToTable("ModelCoefficientSets", Schemas.Wfp);
        b.HasKey(x => x.Id);
        b.Property(x => x.Version).HasMaxLength(ModelCoefficientSet.VersionMaxLength).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(ModelCoefficientSet.CreatedByMaxLength);
        b.HasIndex(x => x.CreatedAt);
        b.HasMany(x => x.Values).WithOne().HasForeignKey(v => v.SetId).OnDelete(DeleteBehavior.Cascade);
    }
}
