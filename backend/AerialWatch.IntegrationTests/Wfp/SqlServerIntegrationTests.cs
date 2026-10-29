using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AerialWatch.Contracts.Wfp.ModelParameters.Models;
using AerialWatch.Contracts.Wfp.ModelParameters.Services;
using AerialWatch.Contracts.Wfp.Planner.Services;
using AerialWatch.Contracts.Wfp.Surveys.Models;
using AerialWatch.Contracts.Wfp.Surveys.Services;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.Infrastructure.Persistence;
using AerialWatch.Application;
using AerialWatch.Infrastructure;

namespace AerialWatch.IntegrationTests.Wfp;

/// <summary>
/// End-to-end tests against a real Microsoft SQL Server. Each test creates its own throw-away database
/// (AerialWatch_Test_xxxx), applies the migrations (what `dotnet ef database update` does) using the normal DI setup,
/// and drops the database afterwards.
/// Server: the local default instance "." (same as appsettings.json); override with environment variable
/// AERIALWATCH_TEST_SQL_SERVER (e.g. "(localdb)\MSSQLLocalDB" or "localhost\SQLEXPRESS").
/// Run only the unit tests with: dotnet test AerialWatch.UnitTests
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
            var server = Environment.GetEnvironmentVariable("AERIALWATCH_TEST_SQL_SERVER") ?? ".";
            var name = $"AerialWatch_Test_{Guid.NewGuid():N}"[..30];
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AerialWatch"] = $"Server={server};Database={name};Trusted_Connection=True;TrustServerCertificate=True"
            }).Build();

            var services = new ServiceCollection().AddApplication().AddInfrastructure(configuration).BuildServiceProvider(); // same wiring as the API
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

        var (types, sets, values) = await db.InScopeAsync(async sp =>
        {
            var ctx = sp.GetRequiredService<AerialWatchDbContext>();
            return (await ctx.CoefficientTypes.CountAsync(), await ctx.ModelCoefficientSets.CountAsync(), await ctx.ModelCoefficientValues.CountAsync());
        });
        Assert.Equal((15, 1, 15), (types, sets, values));

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

        var list = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().ListAsync(new SurveyListQuery()));
        Assert.Single(list.Items);

        await db.InScopeAsync(async sp => { await sp.GetRequiredService<ISurveyService>().DeleteAsync(created.Id); return true; });
        var after = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().ListAsync(new SurveyListQuery()));
        Assert.Empty(after.Items);
        Assert.Equal(0, after.Total);
    }

    [Fact]
    public async Task Survey_list_filters_sorts_and_pages_in_sql()
    {
        await using var db = await TestDatabase.CreateAsync();
        for (var i = 1; i <= 7; i++)
        {
            var site = i % 2 == 0 ? "Katherine" : "Darwin";
            var camera = i % 3 == 0 ? CameraType.Rgb : CameraType.Thermal;
            await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>()
                .CreateAsync(new CreateSurveyRequest($"Survey {i}", site, $"2026-02-0{i}", "18:20", camera)));
        }

        // Every filter, a sort and a page in one query, so EF must translate all of it to SQL.
        var page = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().ListAsync(new SurveyListQuery
        {
            Search = "DARWIN", Camera = CameraType.Thermal, Status = SurveyStatus.Draft, Sort = "surveyDate", Dir = "desc", PageSize = 2,
        }));
        Assert.Equal(3, page.Total); // Darwin = 1,3,5,7; thermal drops 3
        Assert.Equal(new[] { "Survey 7", "Survey 5" }, page.Items.Select(s => s.Name).ToArray());

        var byPhotos = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().ListAsync(new SurveyListQuery { Sort = "imageCount", Date = "2026-02-04" }));
        Assert.Equal("Survey 4", Assert.Single(byPhotos.Items).Name);

        var stats = await db.InScopeAsync(sp => sp.GetRequiredService<ISurveyService>().GetStatsAsync());
        Assert.Equal(7, stats.Surveys);
    }

    [Fact]
    public async Task Saved_coefficients_persist_and_change_the_planner()
    {
        await using var db = await TestDatabase.CreateAsync();

        var current = await db.InScopeAsync(sp => sp.GetRequiredService<IModelParametersService>().GetAsync());
        var edited = current.Coefficients
            .Select(c => new CoefficientEntry(c.Code, c.Code == "thermal.timeSeason.intercept" ? 0.0 : c.Published))
            .ToList();
        await db.InScopeAsync(sp => sp.GetRequiredService<IModelParametersService>().UpdateAsync(new UpdateModelParametersRequest(edited), "integration-test"));

        // New scope = new request: values must come back from SQL Server.
        var plan = await db.InScopeAsync(sp => sp.GetRequiredService<IPlannerService>().GetWindowsAsync("thermal", "wet", null));
        Assert.Equal(0.5, plan.Windows.Single(w => w.Window == TimeWindow.EarlyEvening).Probability, 4);
        Assert.StartsWith("custom-", plan.ModelVersion);

        var history = await db.InScopeAsync(sp => sp.GetRequiredService<IModelParametersService>().GetHistoryAsync());
        Assert.Equal(2, history.Count); // seeded set + this save
        Assert.Equal("integration-test", history[0].SavedBy);
        Assert.Equal(15, history[0].Values.Count);
        Assert.Equal(0.0, history[0].Values.Single(v => v.Code == "thermal.timeSeason.intercept").Value);
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
