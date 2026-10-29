using Xunit;
using AerialWatch.Application.Wfp.Estimates;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Planner;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Estimates.Models;
using AerialWatch.Contracts.Wfp.ModelParameters.Models;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.ModelParameters;

/// <summary>Admin-editable coefficients: defaults, validation, effect on calculations, reset, history.</summary>
public class ModelParametersTests
{
    private const string ThermalIntercept = "thermal.timeSeason.intercept";
    private const string RgbSeasonDry = "rgb.timeSeason.seasonDry";

    private readonly FakeDatabase _db = new();
    private readonly FixedClock _clock = new();

    // A new "request scope" each time, like ASP.NET: fresh repository + active-parameters cache.
    private FakeModelCoefficientSetRepository Sets => new(_db);
    private IActiveModelParameters Active => new ActiveModelParameters(Sets);
    private ModelParametersService Service => new(Active, Sets, new FakeCoefficientTypeRepository(), _db, _clock);

    /// <summary>The published values as a save request, with optional changes by code.</summary>
    private async Task<UpdateModelParametersRequest> RequestAsync(params (string Code, double Value)[] changes)
    {
        var values = (await Service.GetAsync()).Coefficients.ToDictionary(c => c.Code, c => c.Published);
        foreach (var (code, value) in changes) values[code] = value;
        return new UpdateModelParametersRequest(values.Select(kv => new CoefficientEntry(kv.Key, kv.Value)).ToList());
    }

    private static double Current(ModelParametersResponse r, string code) => r.Coefficients.Single(c => c.Code == code).Current;

    private static double EarlyEvening(PlannerResult plan) => plan.Windows.Single(w => w.Window == TimeWindow.EarlyEvening).Probability;

    [Fact]
    public async Task Starts_with_published_coefficients_when_table_is_empty()
    {
        var r = await Service.GetAsync();
        Assert.True(r.IsPublished);
        Assert.Equal(Hvala2023Coefficients.ModelVersion, r.Version);
        Assert.Equal(15, r.Coefficients.Count);
        Assert.Equal(0.951, Current(r, ThermalIntercept), 6);
        Assert.All(r.Coefficients, c => Assert.Equal(c.Published, c.Current));
        Assert.DoesNotContain(r.Coefficients, c => c.Code == "rgb.environment.ambientTemperatureC"); // not in the published RGB model
    }

    [Fact]
    public async Task Coefficients_come_in_display_order_with_labels()
    {
        var r = await Service.GetAsync();
        Assert.Equal(r.Coefficients.OrderBy(c => c.SortOrder).Select(c => c.Code), r.Coefficients.Select(c => c.Code));
        var first = r.Coefficients[0];
        Assert.Equal((CameraType.Thermal, DetectionModelKind.TimeSeason, CoefficientTerm.Intercept, "Baseline"),
            (first.Camera, first.Model, first.Term, first.Label));
    }

    [Fact]
    public async Task Editing_a_coefficient_saves_a_set_with_a_row_per_type_and_changes_planner_output()
    {
        var before = EarlyEvening(await new PlannerService(Active).GetWindowsAsync("thermal", "wet", null));

        var saved = await Service.UpdateAsync(await RequestAsync((ThermalIntercept, 0.0)), updatedBy: "ranger.admin");
        Assert.False(saved.IsPublished);
        Assert.StartsWith("custom-", saved.Version);
        Assert.Equal("ranger.admin", saved.UpdatedBy);
        Assert.Equal(0.0, Current(saved, ThermalIntercept), 6);

        var set = Assert.Single(_db.CoefficientSets);
        Assert.Equal(CoefficientCatalog.All.Select(t => t.Id), set.Values.Select(v => v.CoefficientTypeId).OrderBy(id => id));
        Assert.All(set.Values, v => Assert.Equal(set.Id, v.SetId));
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
        await Service.UpdateAsync(await RequestAsync((ThermalIntercept, 0.0)));
        var r = await new EstimateService(Active).PreviewAsync(new EstimatePreviewRequest(10, CameraType.Thermal, "2026-02-10", "18:20"));
        Assert.Equal(20, r.EstimateRounded); // 10 / 0.5
    }

    [Fact]
    public async Task Reset_adds_a_published_set_and_history_is_newest_first()
    {
        await Service.UpdateAsync(await RequestAsync((RgbSeasonDry, -1)));
        _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
        var r = await Service.ResetToPublishedAsync("ranger.admin");
        Assert.True(r.IsPublished);
        Assert.Equal(-1.8016, Current(r, RgbSeasonDry), 6);

        var history = await Service.GetHistoryAsync();
        Assert.Equal(2, history.Count);
        Assert.True(history[0].IsPublished);
        Assert.False(history[1].IsPublished);
        Assert.Equal(15, history[1].Values.Count);
        Assert.Equal(-1, history[1].Values.Single(v => v.Code == RgbSeasonDry).Value, 6);
    }

    [Fact]
    public async Task Saving_identical_values_is_still_published()
    {
        var r = await Service.UpdateAsync(await RequestAsync());
        Assert.True(r.IsPublished);
        Assert.Equal(Hvala2023Coefficients.ModelVersion, r.Version);
    }

    [Fact]
    public async Task Rejects_bad_missing_unknown_and_duplicate_values_without_saving()
    {
        var request = await RequestAsync((ThermalIntercept, 99), ("rgb.timeSeason.midMorning", double.NaN));
        var values = request.Values!
            .Where(v => v.Code != "thermal.environment.ambientTemperatureC")                 // missing
            .Append(new CoefficientEntry("rgb.environment.ambientTemperatureC", 0.1))        // not a coefficient type
            .Append(new CoefficientEntry(RgbSeasonDry, -1))                                  // given twice
            .ToList();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => Service.UpdateAsync(request with { Values = values }));
        Assert.Contains("between", ex.Errors[ThermalIntercept][0]);
        Assert.Contains("number", ex.Errors["rgb.timeSeason.midMorning"][0]);
        Assert.Contains("required", ex.Errors["thermal.environment.ambientTemperatureC"][0]);
        Assert.Contains("Unknown", ex.Errors["rgb.environment.ambientTemperatureC"][0]);
        Assert.Contains("more than once", ex.Errors[RgbSeasonDry][0]);
        Assert.Empty(_db.CoefficientSets);
        Assert.Equal(0, _db.SaveCalls);
    }

    [Fact]
    public async Task Missing_body_is_rejected()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => Service.UpdateAsync(new UpdateModelParametersRequest(null)));
    }

    [Fact]
    public void Coefficient_set_round_trips_every_value()
    {
        var set = ModelCoefficientSet.From(Hvala2023Coefficients.Published, CoefficientCatalog.All, _clock.UtcNow, "system");
        Assert.True(set.IsPublished);
        Assert.Equal(15, set.Values.Count);
        foreach (var v in set.Values) v.CoefficientType = CoefficientCatalog.All.Single(t => t.Id == v.CoefficientTypeId);
        Assert.Equal(Hvala2023Coefficients.Published, set.ToParameters());
    }

    [Fact]
    public void Incomplete_set_cannot_be_used()
    {
        var set = ModelCoefficientSet.From(Hvala2023Coefficients.Published, CoefficientCatalog.All, _clock.UtcNow, "system");
        foreach (var v in set.Values) v.CoefficientType = CoefficientCatalog.All.Single(t => t.Id == v.CoefficientTypeId);
        set.Values.RemoveAt(0);
        var ex = Assert.Throws<InvalidOperationException>(() => set.ToParameters());
        Assert.Contains("thermal.timeSeason.intercept", ex.Message);
    }

    [Fact]
    public void Catalog_has_one_type_per_coefficient_in_the_published_model()
    {
        Assert.Equal(CoefficientCatalog.All.Count, CoefficientCatalog.All.Select(t => t.Code).Distinct().Count());
        Assert.Equal(CoefficientCatalog.All.Count, CoefficientCatalog.All.Select(t => t.Id).Distinct().Count());
        foreach (var t in CoefficientCatalog.All)
            Assert.NotNull(Hvala2023Coefficients.Published.ValueOf(t.Camera, t.Model, t.Term));
    }
}
