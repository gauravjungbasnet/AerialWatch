using Microsoft.EntityFrameworkCore;
using AerialWatch.Domain.Entities;

namespace AerialWatch.Persistence;

/// <summary>EF Core context for Microsoft SQL Server. Table mappings live in Configurations/, schema changes in Migrations/.</summary>
public class AerialWatchDbContext : DbContext
{
    public AerialWatchDbContext(DbContextOptions<AerialWatchDbContext> options) : base(options)
    {
    }

    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyImage> SurveyImages => Set<SurveyImage>();
    public DbSet<AnimalDetection> Detections => Set<AnimalDetection>();
    public DbSet<ModelCoefficientSet> ModelCoefficientSets => Set<ModelCoefficientSet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AerialWatchDbContext).Assembly);
    }
}
