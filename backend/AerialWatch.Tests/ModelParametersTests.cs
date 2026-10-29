using AerialWatch.Application.Common;
using AerialWatch.Application.Estimates;
using AerialWatch.Application.ModelParameters;
using AerialWatch.Application.Planner;
using AerialWatch.Core.Detection;
using Xunit;

namespace AerialWatch.Tests;

/// <summary>Admin-editable coefficients: defaults, validation, effect on calculations, reset, history.</summary>
public class ModelParametersTests
{
    private readonly FakeDatabase _db = new();
    private readonly FixedClock _clock = new();

    // A new "request scope" each time, like ASP.NET: fresh repository + active-parameters cache.
    private FakeModelCoefficientSetRepository Sets => new(_db);
    private IActiveModelParameters Active => new ActiveModelParameters(Sets);
    private ModelParametersService Service => new(Active, Sets, _db, _clock);

    private async Task<ModelParameterSet> PublishedAsync() => (await Service.GetAsync()).Published;

    private static double EarlyEvening(PlannerResult plan) => plan.Windows.Single(w => w.Window == TimeWindow.EarlyEvening).Probability;

    [Fact]
    public async Task Starts_with_published_coefficients_when_table_is_empty()
    {
        var r = await Service.GetAsync();
        Assert.True(r.IsPublished);
        Assert.Equal(Hvala2023Coefficients.ModelVersion, r.Version);
        Assert.Equal(0.951, r.Current.ThermalTimeSeason.Intercept, 6);
        Assert.Null(r.Current.RgbEnvironment.AmbientTemperatureC);
    }

    [Fact]
    public async Task Editing_a_coefficient_saves_a_row_changes_planner_output_and_adds_warning()
    {
        var before = EarlyEvening(await new PlannerService(Active).GetWindowsAsync("thermal", "wet", null));
        var published = await PublishedAsync();

        var edited = published with { ThermalTimeSeason = published.ThermalTimeSeason with { Intercept = 0.0 } };
        var saved = await Service.UpdateAsync(new UpdateModelParametersRequest(edited), updatedBy: "ranger.admin");
        Assert.False(saved.IsPublished);
        Assert.StartsWith("custom-", saved.Version);
        Assert.Equal("ranger.admin", saved.UpdatedBy);
        Assert.Single(_db.CoefficientSets);
        Assert.Equal(1, _db.SaveCalls);

        var plan = await new PlannerService(Active).GetWindowsAsync("thermal", "wet", null);
        var after = EarlyEvening(plan);
        Assert.Equal(0.5, after, 4); // logistic(0)
        Assert.True(after < before);
        Assert.Contains(plan.Assumptions, a => a.Level == AssumptionLevel.Warning && a.Message.Contains("edited"));
        Assert.StartsWith("custom-", plan.ModelVersion);
    }

    [Fact]
    public async Task Estimates_use_edited_coefficients()
    {
        var published = await PublishedAsync();
        await Service.UpdateAsync(new UpdateModelParametersRequest(published with { ThermalTimeSeason = published.ThermalTimeSeason with { Intercept = 0.0 } }));
        var r = await new EstimateService(Active).PreviewAsync(new EstimatePreviewRequest(10, CameraType.Thermal, "2026-02-10", "18:20"));
        Assert.Equal(20, r.EstimateRounded); // 10 / 0.5
    }

    [Fact]
    public async Task Reset_adds_a_published_row_and_history_is_newest_first()
    {
        var published = await PublishedAsync();
        await Service.UpdateAsync(new UpdateModelParametersRequest(published with { RgbTimeSeason = published.RgbTimeSeason with { SeasonDry = -1 } }));
        _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
        var r = await Service.ResetToPublishedAsync("ranger.admin");
        Assert.True(r.IsPublished);
        Assert.Equal(-1.8016, r.Current.RgbTimeSeason.SeasonDry, 6);

        var history = await Service.GetHistoryAsync();
        Assert.Equal(2, history.Count);
        Assert.True(history[0].IsPublished);
        Assert.False(history[1].IsPublished);
        Assert.Equal(-1, history[1].Values.RgbTimeSeason.SeasonDry, 6);
    }

    [Fact]
    public async Task Saving_identical_values_is_still_published()
    {
        var r = await Service.UpdateAsync(new UpdateModelParametersRequest(await PublishedAsync()));
        Assert.True(r.IsPublished);
        Assert.Equal(Hvala2023Coefficients.ModelVersion, r.Version);
    }

    [Fact]
    public async Task Rejects_out_of_range_and_invalid_values_without_saving()
    {
        var published = await PublishedAsync();
        var bad = published with
        {
            ThermalTimeSeason = published.ThermalTimeSeason with { Intercept = 99 },
            RgbTimeSeason = published.RgbTimeSeason with { MidMorning = double.NaN },
            ThermalEnvironment = published.ThermalEnvironment with { AmbientTemperatureC = null },
            RgbEnvironment = published.RgbEnvironment with { AmbientTemperatureC = 0.1 },
        };
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => Service.UpdateAsync(new UpdateModelParametersRequest(bad)));
        Assert.True(ex.Errors.ContainsKey("thermalTimeSeason.intercept"));
        Assert.True(ex.Errors.ContainsKey("rgbTimeSeason.midMorning"));
        Assert.True(ex.Errors.ContainsKey("thermalEnvironment.ambientTemperatureC"));
        Assert.True(ex.Errors.ContainsKey("rgbEnvironment.ambientTemperatureC"));
        Assert.Empty(_db.CoefficientSets);
        Assert.Equal(0, _db.SaveCalls);
    }

    [Fact]
    public async Task Missing_body_is_rejected()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => Service.UpdateAsync(new UpdateModelParametersRequest(null)));
    }

    [Fact]
    public void Coefficient_row_round_trips_every_value()
    {
        var row = Core.Entities.ModelCoefficientSet.From(Hvala2023Coefficients.Published, _clock.UtcNow, "system");
        Assert.True(row.IsPublished);
        Assert.Equal(Hvala2023Coefficients.Published, row.ToParameters());
    }
}
