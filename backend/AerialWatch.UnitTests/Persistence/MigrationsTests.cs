using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Infrastructure.Persistence;
using AerialWatch.Infrastructure.Persistence.Migrations;

namespace AerialWatch.UnitTests.Persistence;

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
        // (dotnet ef migrations add <Name> --project AerialWatch.Infrastructure --startup-project AerialWatch.Api --output-dir Persistence\\Migrations).
        Assert.True(differences.Count == 0,
            "Model has changes not in a migration: " + string.Join(", ", differences.Select(d => d.GetType().Name)));
    }

    [Fact]
    public void Migrations_create_every_table()
    {
        using var db = NewContext();
        // Every migration, in order. Add a new migration's id here (and to docs/08 §4.5) when you create one.
        Assert.Equal(new[]
        {
            "20261024000000_InitialCreate",
            "20261024000100_SeedPublishedCoefficients",
            "20261025004756_AddSurveyImageFiles",          // photo storage columns (D-030)
            "20261025100000_AddReviewFields",              // review audit + "second look" flag (D-034)
            "20261026000000_AddErrorLogs",                 // error logging table
            "20261026101809_AddTrainingSplit",             // AI training split per survey (D-038)
            "20261029000000_AddImagePairs",                // thermal + colour twins (D-039)
            "20261029010000_AddTwinMarks",                 // pig positions on the twin photo (D-040)
            "20261029020000_AddTwinSightings"              // pigs drawn first on the twin photo (D-041)
        }, db.Database.GetMigrations().ToArray());

        var script = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE SCHEMA [wfp]", script);
        foreach (var table in new[] { "Surveys", "SurveyImages", "Detections", "CoefficientTypes", "ModelCoefficientSets", "ModelCoefficientValues", "TwinMarks" })
            Assert.Contains($"CREATE TABLE [wfp].[{table}]", script);

        // Photo storage (D-030): new columns and the per-survey duplicate check
        foreach (var column in new[] { "ThumbnailPath", "ContentType", "Sha256" })
            Assert.Contains($"[{column}]", script);
        Assert.Contains("IX_SurveyImages_SurveyId_Sha256", script);

        // Review (D-034): who checked each photo/box and when, and the "needs a second look" flag
        Assert.Contains("[Flagged] bit NOT NULL", script);
        Assert.Contains("ALTER TABLE [wfp].[Detections] ADD [ReviewedBy] nvarchar(100)", script);
        Assert.Contains("ALTER TABLE [wfp].[SurveyImages] ADD [ReviewedAt] datetimeoffset", script);
        // Training data (D-038): which set a survey's photos are exported to
        Assert.Contains("ALTER TABLE [wfp].[Surveys] ADD [TrainingSplit] nvarchar(12) NULL", script);
        // Thermal + colour twins (D-039): camera per photo, pair key and its index, GPS copied from the twin
        Assert.Contains("ALTER TABLE [wfp].[SurveyImages] ADD [PairKey] nvarchar(260) NULL", script);
        Assert.Contains("ALTER TABLE [wfp].[SurveyImages] ADD [LocationFromTwin] bit NOT NULL", script);
        Assert.Contains("IX_SurveyImages_SurveyId_PairKey", script);
        // Twin check (D-040): one mark per box, deleted with its box
        Assert.Contains("IX_TwinMarks_DetectionId", script);
        Assert.Contains("FK_TwinMarks_Detections_DetectionId", script);
        // Twin sightings (D-041): a mark may exist before its counted box
        Assert.Contains("ALTER TABLE [wfp].[TwinMarks] ADD [CountedImageId] uniqueidentifier NOT NULL", script);
        Assert.Contains("WHERE [DetectionId] IS NOT NULL", script);
    }

    [Fact]
    public void Every_table_lives_in_a_module_schema_never_dbo()
    {
        using var db = NewContext();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var schema = entity.GetSchema();
            Assert.True(schema is not null && schema != "dbo",
                $"{entity.ClrType.Name} must be mapped to a module schema (Schemas.Wfp, ...), not dbo");
            Assert.True(typeof(Schemas).GetFields().Any(f => (string?)f.GetValue(null) == schema),
                $"{entity.ClrType.Name} uses schema '{schema}', which is not declared in Persistence/Schemas.cs");
        }
    }

    [Fact]
    public void Seed_migration_matches_catalog_and_published_coefficients()
    {
        var inserts = new SeedPublishedCoefficients().UpOperations.Cast<InsertDataOperation>().ToDictionary(o => o.Table);
        Assert.All(inserts.Values, o => Assert.Equal("wfp", o.Schema));

        // 1. Coefficient types = CoefficientCatalog.All
        var types = Rows(inserts["CoefficientTypes"]);
        Assert.Equal(CoefficientCatalog.All.Count, types.Count);
        foreach (var (row, expected) in types.Zip(CoefficientCatalog.All))
        {
            Assert.Equal(expected.Id, row["Id"]);
            Assert.Equal(expected.Code, row["Code"]);
            Assert.Equal(expected.Camera.ToString(), row["Camera"]);
            Assert.Equal(expected.Model.ToString(), row["Model"]);
            Assert.Equal(expected.Term.ToString(), row["Term"]);
            Assert.Equal(expected.Label, row["Label"]);
            Assert.Equal(expected.Description, row["Description"]);
            Assert.Equal(expected.SortOrder, row["SortOrder"]);
        }

        // 2. The seeded set is published and holds exactly the published value for every type
        var set = Assert.Single(Rows(inserts["ModelCoefficientSets"]));
        Assert.Equal(SeedPublishedCoefficients.SeedSetId, set["Id"]);
        Assert.Equal(Hvala2023Coefficients.ModelVersion, set["Version"]);
        Assert.Equal(true, set["IsPublished"]);

        var values = Rows(inserts["ModelCoefficientValues"]);
        Assert.Equal(CoefficientCatalog.All.Count, values.Count);
        foreach (var type in CoefficientCatalog.All)
        {
            var row = Assert.Single(values, v => (int)v["CoefficientTypeId"]! == type.Id);
            Assert.Equal(SeedPublishedCoefficients.SeedSetId, row["SetId"]);
            var published = Hvala2023Coefficients.Published.ValueOf(type.Camera, type.Model, type.Term);
            Assert.True(Equals(row["Value"], published), $"{type.Code}: migration has {row["Value"]}, published is {published}");
        }
    }

    private static List<Dictionary<string, object?>> Rows(InsertDataOperation insert) =>
        Enumerable.Range(0, insert.Values.GetLength(0))
            .Select(r => insert.Columns.Select((c, i) => (c, v: insert.Values[r, i])).ToDictionary(x => x.c, x => x.v))
            .ToList();
}
