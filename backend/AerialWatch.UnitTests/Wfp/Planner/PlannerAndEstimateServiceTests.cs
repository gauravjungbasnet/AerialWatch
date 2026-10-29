using Xunit;
using AerialWatch.Application.Wfp.Estimates;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Planner;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Estimates.Models;
using AerialWatch.Contracts.Wfp.Planner.Models;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.Planner;

public class PlannerAndEstimateServiceTests
{
    private static IActiveModelParameters Published() => new ActiveModelParameters(new FakeModelCoefficientSetRepository(new FakeDatabase()));

    [Fact]
    public async Task Planner_service_accepts_case_insensitive_query_values()
    {
        var plan = await new PlannerService(Published()).GetWindowsAsync("Thermal", "DRY", null);
        Assert.Equal(Season.Dry, plan.Season);
        Assert.Equal(TimeWindow.EarlyEvening, plan.Windows.Single(w => w.IsBest).Window);
    }

    [Fact]
    public async Task Planner_service_reports_all_field_errors()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => new PlannerService(Published()).GetWindowsAsync("drone", "hot", null));
        Assert.True(ex.Errors.ContainsKey("camera"));
        Assert.True(ex.Errors.ContainsKey("season"));
    }

    [Fact]
    public async Task Estimate_service_matches_research_model()
    {
        var r = await new EstimateService(Published()).PreviewAsync(new EstimatePreviewRequest(15, CameraType.Thermal, "2026-02-10", "18:20", AreaKm2: 5));
        Assert.Equal(21, r.EstimateRounded);
        Assert.Equal(TimeWindow.EarlyEvening, r.Window);
        Assert.Equal(4.16, r.DensityPerKm2);
    }

    [Fact]
    public async Task Estimate_service_rejects_night_surveys()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            new EstimateService(Published()).PreviewAsync(new EstimatePreviewRequest(5, CameraType.Rgb, "2026-02-10", "23:00")));
        Assert.True(ex.Errors.ContainsKey("startTime"));
    }

    [Fact]
    public async Task Environment_probability_requires_temperature_for_thermal()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            new PlannerService(Published()).GetProbabilityAsync(new ProbabilityRequest(CameraType.Thermal, null, null, null, null,
                DetectionModelKind.Environment, null, 0.2)));
        Assert.True(ex.Errors.ContainsKey("ambientTemperatureC"));
    }
}
