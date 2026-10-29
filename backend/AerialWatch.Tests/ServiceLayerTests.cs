using AerialWatch.Application.Abstractions;
using AerialWatch.Application.Common;
using AerialWatch.Application.Estimates;
using AerialWatch.Application.Planner;
using AerialWatch.Application.Surveys;
using AerialWatch.Core.Detection;
using AerialWatch.Core.Entities;
using AerialWatch.Application.ModelParameters;
using Xunit;

namespace AerialWatch.Tests;

/// <summary>Service layer tests using repository test doubles (no database needed).</summary>
public class SurveyServiceTests
{
    private readonly FixedClock _clock = new();
    private readonly SurveyService _service;

    public SurveyServiceTests()
    {
        var db = new FakeDatabase();
        _service = new SurveyService(new FakeSurveyRepository(db), db, _clock,
            new ActiveModelParameters(new FakeModelCoefficientSetRepository(db)));
    }

    private static CreateSurveyRequest Valid(string name = "North paddock", string time = "18:20", string date = "2026-02-10") =>
        new(name, "Katherine", date, time, CameraType.Thermal, AltitudeM: 52);

    [Fact]
    public async Task Create_derives_season_window_and_detection_probability()
    {
        var created = await _service.CreateAsync(Valid());

        Assert.Equal(Season.Wet, created.Season);
        Assert.Equal(TimeWindow.EarlyEvening, created.TimeWindow);
        Assert.Equal(SurveyStatus.Draft, created.Status);
        Assert.NotNull(created.DetectionProbability);
        Assert.Equal(0.7213, created.DetectionProbability!.Value, 3);
    }

    [Fact]
    public async Task Create_trims_and_persists_so_get_returns_it()
    {
        var created = await _service.CreateAsync(Valid(name: "  Creek line  "));
        var fetched = await _service.GetAsync(created.Id);
        Assert.Equal("Creek line", fetched.Name);
    }

    [Fact]
    public async Task Create_at_night_has_no_window_and_warns()
    {
        var created = await _service.CreateAsync(Valid(time: "22:30"));
        Assert.Null(created.TimeWindow);
        Assert.Null(created.DetectionProbability);
        Assert.Contains(created.Assumptions, a => a.Level == AssumptionLevel.Warning);
    }

    [Fact]
    public async Task Create_rejects_invalid_input_with_field_errors()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.CreateAsync(new CreateSurveyRequest("", "", "10/02/2026", "25:00", CameraType.Rgb, AltitudeM: 0)));

        foreach (var field in new[] { "name", "siteName", "surveyDate", "startTime", "altitudeM" })
            Assert.True(ex.Errors.ContainsKey(field), $"Expected an error for {field}");
    }

    [Fact]
    public async Task Altitude_far_from_study_adds_warning()
    {
        var created = await _service.CreateAsync(Valid() with { AltitudeM = 120 });
        Assert.Contains(created.Assumptions, a => a.Level == AssumptionLevel.Warning && a.Message.Contains("altitude"));
    }

    [Fact]
    public async Task List_returns_most_recent_first()
    {
        await _service.CreateAsync(Valid(name: "First"));
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        await _service.CreateAsync(Valid(name: "Second"));

        var list = await _service.ListAsync();
        Assert.Equal(new[] { "Second", "First" }, list.Select(s => s.Name).ToArray());
    }

    [Fact]
    public async Task Delete_removes_and_missing_survey_is_not_found()
    {
        var created = await _service.CreateAsync(Valid());
        await _service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(created.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }
}

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
