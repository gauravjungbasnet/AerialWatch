using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application;
using AerialWatch.Application.ModelParameters;
using AerialWatch.Application.Planner;
using AerialWatch.Application.Surveys;
using AerialWatch.Core.Detection;
using AerialWatch.Core.Entities;
using AerialWatch.Infrastructure;
using AerialWatch.Infrastructure.Persistence;
using Xunit;

namespace AerialWatch.Tests.SqlServer;

/// <summary>
/// End-to-end tests against a real Microsoft SQL Server. Each test creates its own throw-away database
/// (AerialWatch_Test_xxxx), applies the migrations (what `dotnet ef database update` does) using the normal DI setup,
/// and drops the database afterwards.
/// Server: LocalDB by default; override with environment variable AERIALWATCH_TEST_SQL_SERVER (e.g. "localhost\SQLEXPRESS").
/// Skip them with: dotnet test --filter "Category!=SqlServer"
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerIntegrationTests
{
    private sealed class TestDatabase : IAsyncDisposable
    {
        public ServiceProvider Services { get; }

        private TestDatabase(ServiceProvider services) => Services = services;

        public static async Task<TestDatabase> CreateAsync()
        {
            var server = Environment.GetEnvironmentVariable("AERIALWATCH_TEST_SQL_SERVER") ?? "(localdb)\\MSSQLLocalDB";
            var name = $"AerialWatch_Test_{Guid.NewGuid():N}"[..30];
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AerialWatch"] = $"Server={server};Database={name};Trusted_Connection=True;TrustServerCertificate=True"
            }).Build();

            var services = new ServiceCollection().AddApplication().AddInfrastructure(configuration).BuildServiceProvider();
            await using (var scope = services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<AerialWatchDbContext>().Database.MigrateAsync(); // = dotnet ef database update
            return new TestDatabase(services);
        }

        public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
        {
            await using var scope = Services.CreateAsyncScope(); // one scope = one HTTP request
            return await work(scope.ServiceProvider);
        }

        public async ValueTask DisposeAsync()
        {
            await using (var scope = Services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<AerialWatchDbContext>().Database.EnsureDeletedAsync();
            await Services.DisposeAsync();
        }
    }

    [Fact]
    public async Task Migrations_create_schema_and_seed_published_coefficients()
    {
        await using var db = await TestDatabase.CreateAsync();

        var applied = await db.InScopeAsync(sp => sp.GetRequiredService<AerialWatchDbContext>().Database.GetAppliedMigrationsAsync());
        Assert.Contains("20261024000000_InitialCreate", applied);
        Assert.Contains("20261024000100_SeedPublishedCoefficients", applied);

        var rows = await db.InScopeAsync(sp => sp.GetRequiredService<AerialWatchDbContext>().ModelCoefficientSets.CountAsync());
        Assert.Equal(1, rows);

        var current = await db.InScopeAsync(sp => sp.GetRequiredService<IModelParametersService>().GetAsync());
        Assert.True(current.IsPublished);
        Assert.Equal("system", current.UpdatedBy);
    }

    [Fact]
    public async Task Survey_create_get_list_delete_round_trip()
    {
        await using var db = await TestDatabase.CreateAsync();
        var request = new CreateSurveyRequest("North paddock", "Katherine", "2026-02-10", "18:20", CameraType.Thermal, AltitudeM: 52, AreaKm2: 5);

        var created = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().CreateAsync(request));
        var fetched = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().GetAsync(created.Id));
        Assert.Equal("North paddock", fetched.Name);
        Assert.Equal("18:20", fetched.StartTime);
        Assert.Equal(TimeWindow.EarlyEvening, fetched.TimeWindow);
        Assert.Equal(0.7213, fetched.DetectionProbability!.Value, 3);

        var list = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().ListAsync());
        Assert.Single(list);

        await db.InScopeAsync(async sp => { await sp.GetRequiredService<ISurveyService>().DeleteAsync(created.Id); return true; });
        var after = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().ListAsync());
        Assert.Empty(after);
    }

    [Fact]
    public async Task Saved_coefficients_persist_and_change_the_planner()
    {
        await using var db = await TestDatabase.CreateAsync();

        var published = await db.InScopeAsync(async sp => (await sp.GetRequiredService<IModelParametersService>().GetAsync()).Published);
        var edited = published with { ThermalTimeSeason = published.ThermalTimeSeason with { Intercept = 0.0 } };
        await db.InScopeAsync(sp => sp.GetRequiredService<IModelParametersService>().UpdateAsync(new UpdateModelParametersRequest(edited), "integration-test"));

        // New scope = new request: values must come back from SQL Server.
        var plan = await db.InScopeAsync(sp => sp.GetRequiredService<IPlannerService>().GetWindowsAsync("thermal", "wet", null));
        Assert.Equal(0.5, plan.Windows.Single(w => w.Window == TimeWindow.EarlyEvening).Probability, 4);
        Assert.StartsWith("custom-", plan.ModelVersion);

        var history = await db.InScopeAsync(sp => sp.GetRequiredService<IModelParametersService>().GetHistoryAsync());
        Assert.Equal(2, history.Count); // seeded row + this save
        Assert.Equal("integration-test", history[0].SavedBy);
    }

    [Fact]
    public async Task Deleting_a_survey_cascades_to_images_and_detections()
    {
        await using var db = await TestDatabase.CreateAsync();
        var surveyId = await db.InScopeAsync(async sp =>
        {
            var ctx = sp.GetRequiredService<AerialWatchDbContext>();
            var now = DateTimeOffset.UtcNow;
            var survey = new Survey { Name = "Cascade", SiteName = "Test", SurveyDate = new DateOnly(2026, 2, 10), StartTime = new TimeOnly(18, 20), CreatedAt = now, UpdatedAt = now };
            var image = new SurveyImage { FileName = "a.jpg", StoragePath = "x/a.jpg", CreatedAt = now, UpdatedAt = now };
            image.Detections.Add(new AnimalDetection { Label = "feral_pig", X = 1, Y = 1, Width = 2, Height = 2, CreatedAt = now, UpdatedAt = now });
            survey.Images.Add(image);
            ctx.Surveys.Add(survey);
            await ctx.SaveChangesAsync();
            return survey.Id;
        });

        await db.InScopeAsync(async sp => { await sp.GetRequiredService<ISurveyService>().DeleteAsync(surveyId); return true; });

        var remaining = await db.InScopeAsync(async sp =>
        {
            var ctx = sp.GetRequiredService<AerialWatchDbContext>();
            return await ctx.SurveyImages.CountAsync() + await ctx.Detections.CountAsync();
        });
        Assert.Equal(0, remaining);
    }
}
