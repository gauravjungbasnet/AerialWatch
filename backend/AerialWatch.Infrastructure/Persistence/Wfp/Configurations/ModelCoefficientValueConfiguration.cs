using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Configurations;

/// <summary>One value per coefficient type per saved set. A coefficient type can't be deleted while values use it.</summary>
public sealed class ModelCoefficientValueConfiguration : IEntityTypeConfiguration<ModelCoefficientValue>
{
    public void Configure(EntityTypeBuilder<ModelCoefficientValue> b)
    {
        b.ToTable("ModelCoefficientValues", Schemas.Wfp);
        b.HasKey(x => new { x.SetId, x.CoefficientTypeId });
        b.HasOne(x => x.CoefficientType).WithMany().HasForeignKey(x => x.CoefficientTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
