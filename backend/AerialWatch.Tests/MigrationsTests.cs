using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using AerialWatch.Core.Detection;
using AerialWatch.Infrastructure.Persistence;
using AerialWatch.Infrastructure.Persistence.Migrations;
using Xunit;

namespace AerialWatch.Tests;

/// <summary>Schema checks that need no SQL Server connection: migrations must match the EF model exactly.</summary>
public class MigrationsTests
{
    private static AerialWatchDbContext NewContext() => new(new DbContextOptionsBuilder<AerialWatchDbContext>()
        .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AerialWatch_ModelCheck;Trusted_Connection=True")
        .Options);

    [Fact]
    public void Snapshot_matches_current_model()
    {
        using var db = NewContext();
        var migrations = db.GetService<IMigrationsAssembly>();
        IModel? snapshot = migrations.ModelSnapshot?.Model;
        if (snapshot is IMutableModel mutable) snapshot = mutable.FinalizeModel();
        if (snapshot is not null) snapshot = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot);

        var differences = db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(snapshot?.GetRelationalModel(), db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        // If this fails, the entity model changed: add a migration
        // (dotnet ef migrations add <Name> --project AerialWatch.Infrastructure --output-dir Persistence/Migrations).
        Assert.True(differences.Count == 0,
            "Model has changes not in a migration: " + string.Join(", ", differences.Select(d => d.GetType().Name)));
    }

    [Fact]
    public void Initial_migration_creates_every_table()
    {
        using var db = NewContext();
        Assert.Equal(new[] { "20261024000000_InitialCreate", "20261024000100_SeedPublishedCoefficients" }, db.Database.GetMigrations().ToArray());

        var script = db.GetService<IMigrator>().GenerateScript();
        foreach (var table in new[] { "Surveys", "SurveyImages", "Detections", "ModelCoefficientSets" })
            Assert.Contains($"CREATE TABLE [{table}]", script);
    }

    [Fact]
    public void Seed_migration_matches_published_coefficients()
    {
        var insert = (InsertDataOperation)new SeedPublishedCoefficients().UpOperations.Single();
        Assert.Equal("ModelCoefficientSets", insert.Table);
        var row = insert.Columns.Select((c, i) => (c, v: insert.Values[0, i])).ToDictionary(x => x.c, x => x.v);

        var p = Hvala2023Coefficients.Published;
        var expected = new Dictionary<string, object?>
        {
            ["Version"] = p.Version, ["IsPublished"] = true,
            ["ThermalIntercept"] = p.ThermalTimeSeason.Intercept, ["ThermalSeasonDry"] = p.ThermalTimeSeason.SeasonDry,
            ["ThermalMidAfternoon"] = p.ThermalTimeSeason.MidAfternoon, ["ThermalMidMorning"] = p.ThermalTimeSeason.MidMorning,
            ["ThermalEarlyMorning"] = p.ThermalTimeSeason.EarlyMorning,
            ["RgbIntercept"] = p.RgbTimeSeason.Intercept, ["RgbSeasonDry"] = p.RgbTimeSeason.SeasonDry,
            ["RgbMidAfternoon"] = p.RgbTimeSeason.MidAfternoon, ["RgbMidMorning"] = p.RgbTimeSeason.MidMorning,
            ["RgbEarlyMorning"] = p.RgbTimeSeason.EarlyMorning,
            ["ThermalEnvIntercept"] = p.ThermalEnvironment.Intercept, ["ThermalEnvTemperatureC"] = p.ThermalEnvironment.AmbientTemperatureC,
            ["ThermalEnvCanopy"] = p.ThermalEnvironment.TimeUnderCanopy,
            ["RgbEnvIntercept"] = p.RgbEnvironment.Intercept, ["RgbEnvTemperatureC"] = p.RgbEnvironment.AmbientTemperatureC,
            ["RgbEnvCanopy"] = p.RgbEnvironment.TimeUnderCanopy,
        };
        foreach (var (column, value) in expected)
            Assert.True(Equals(row[column], value), $"{column}: migration has {row[column]}, published is {value}");
    }
}
