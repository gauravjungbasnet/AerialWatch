using Xunit;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.UnitTests.Wfp.ResearchModel;

/// <summary>
/// Checks our implementation of Hvala et al. (2023) against the paper's reported findings.
/// Evidence for the judges: the model behaves as the published research describes.
/// </summary>
public class DetectionModelTests
{
    private static double P(TimeWindow w, Season s, CameraType c) =>
        DetectionProbabilityModel.TimeSeason(w, s, c).Probability;

    public static IEnumerable<object[]> AllSeasonCameraCombos() =>
        from s in new[] { Season.Wet, Season.Dry }
        from c in new[] { CameraType.Thermal, CameraType.Rgb }
        select new object[] { s, c };

    [Fact]
    public void Logistic_of_zero_is_one_half()
    {
        Assert.Equal(0.5, DetectionProbabilityModel.Logistic(0), 6);
    }

    [Fact]
    public void Thermal_early_evening_wet_matches_intercept()
    {
        // Reference level: logit = intercept 0.951 → p ≈ 0.7213
        Assert.Equal(0.7213, P(TimeWindow.EarlyEvening, Season.Wet, CameraType.Thermal), 3);
    }

    [Theory]
    [MemberData(nameof(AllSeasonCameraCombos))]
    public void Mid_afternoon_is_the_worst_window(Season season, CameraType camera)
    {
        // Paper: detection lowest in mid-afternoon.
        var afternoon = P(TimeWindow.MidAfternoon, season, camera);
        foreach (var w in new[] { TimeWindow.EarlyMorning, TimeWindow.MidMorning, TimeWindow.EarlyEvening })
            Assert.True(afternoon < P(w, season, camera), $"{w} should beat mid-afternoon ({season}, {camera})");
    }

    [Theory]
    [MemberData(nameof(AllSeasonCameraCombos))]
    public void Early_evening_is_the_best_window(Season season, CameraType camera)
    {
        // Paper: detection highest in early evening.
        var evening = P(TimeWindow.EarlyEvening, season, camera);
        foreach (var w in new[] { TimeWindow.EarlyMorning, TimeWindow.MidMorning, TimeWindow.MidAfternoon })
            Assert.True(evening > P(w, season, camera), $"Early evening should beat {w} ({season}, {camera})");
    }

    [Theory]
    [MemberData(nameof(AllSeasonCameraCombos))]
    public void Mid_afternoon_detection_is_low(Season season, CameraType camera)
    {
        // Paper: mid-afternoon roughly 5–20% (4.8–14.3% by season/camera). Allow a small tolerance.
        Assert.InRange(P(TimeWindow.MidAfternoon, season, camera), 0.03, 0.21);
    }

    [Theory]
    [InlineData(CameraType.Thermal)]
    [InlineData(CameraType.Rgb)]
    public void Wet_season_beats_dry_season_in_every_window(CameraType camera)
    {
        // Paper: detection ~22–23% higher in the wet season.
        foreach (var w in Enum.GetValues<TimeWindow>())
            Assert.True(P(w, Season.Wet, camera) > P(w, Season.Dry, camera), $"Wet should beat dry at {w} ({camera})");
    }

    [Theory]
    [InlineData(Season.Wet)]
    [InlineData(Season.Dry)]
    public void Thermal_detects_fewer_than_rgb_in_early_evening(Season season)
    {
        // Paper: thermal recorded ~20% fewer detections than RGB in early evening.
        Assert.True(P(TimeWindow.EarlyEvening, season, CameraType.Thermal) < P(TimeWindow.EarlyEvening, season, CameraType.Rgb));
    }

    [Fact]
    public void Rgb_dry_season_early_morning_carries_low_light_warning()
    {
        var result = DetectionProbabilityModel.TimeSeason(TimeWindow.EarlyMorning, Season.Dry, CameraType.Rgb);
        Assert.Contains(result.Assumptions, a => a.Level == AssumptionLevel.Warning && a.Message.Contains("light"));
    }

    [Fact]
    public void Environment_model_open_ground_rgb()
    {
        // canopy = 0 → logit = 1.2789 → p ≈ 0.782
        var r = DetectionProbabilityModel.Environment(CameraType.Rgb, 0, null);
        Assert.Equal(0.782, r.Probability, 3);
        Assert.Equal(DetectionModelKind.Environment, r.Model);
    }

    [Fact]
    public void Environment_model_thermal_gets_worse_when_hot()
    {
        var mild = DetectionProbabilityModel.Environment(CameraType.Thermal, 0, 20).Probability;
        var hot = DetectionProbabilityModel.Environment(CameraType.Thermal, 0, 35).Probability;
        Assert.True(hot < mild);
        Assert.Equal(0.650, DetectionProbabilityModel.Environment(CameraType.Thermal, 0, 30).Probability, 3);
    }

    [Fact]
    public void Environment_model_more_canopy_means_lower_detection()
    {
        var open = DetectionProbabilityModel.Environment(CameraType.Rgb, 0, null).Probability;
        var dense = DetectionProbabilityModel.Environment(CameraType.Rgb, 0.9, null).Probability;
        Assert.True(dense < open);
    }

    [Fact]
    public void Environment_model_thermal_requires_temperature()
    {
        Assert.Throws<ArgumentException>(() => DetectionProbabilityModel.Environment(CameraType.Thermal, 0.2, null));
    }

    [Fact]
    public void Environment_model_rejects_invalid_canopy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DetectionProbabilityModel.Environment(CameraType.Rgb, 1.5, null));
    }
}
