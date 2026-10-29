using AerialWatch.Core.Detection;
using Xunit;

namespace AerialWatch.Tests;

public class PopulationEstimatorTests
{
    [Fact]
    public void Fifteen_seen_at_65_percent_is_about_23()
    {
        var e = PopulationEstimator.Estimate(15, 0.65);
        Assert.Equal(23, e.EstimateRounded);
        Assert.InRange(e.Low, 15, 23);
        Assert.InRange(e.High, 23, 40);
    }

    [Fact]
    public void Low_detection_gives_much_larger_and_wider_estimate()
    {
        var evening = PopulationEstimator.Estimate(15, 0.65);
        var afternoon = PopulationEstimator.Estimate(15, 0.10);
        Assert.Equal(150, afternoon.EstimateRounded);
        Assert.True(afternoon.High - afternoon.Low > evening.High - evening.Low);
    }

    [Fact]
    public void Range_never_goes_below_confirmed_count()
    {
        var e = PopulationEstimator.Estimate(3, 0.2);
        Assert.True(e.Low >= 3);
    }

    [Fact]
    public void Perfect_detection_returns_the_count()
    {
        var e = PopulationEstimator.Estimate(12, 1.0);
        Assert.Equal(12, e.EstimateRounded);
        Assert.Equal(12, e.Low);
        Assert.Equal(12, e.High);
    }

    [Fact]
    public void Zero_seen_gives_zero_with_an_upper_bound()
    {
        var e = PopulationEstimator.Estimate(0, 0.5);
        Assert.Equal(0, e.EstimateRounded);
        Assert.Equal(4, e.High); // ln(0.05)/ln(0.5) = 4.32
        Assert.Contains("does not prove", e.Method);
    }

    [Fact]
    public void Density_uses_area()
    {
        var e = PopulationEstimator.Estimate(10, 0.5, areaKm2: 4);
        Assert.Equal(5.0, e.DensityPerKm2);
    }

    [Theory]
    [InlineData(-1, 0.5)]
    [InlineData(5, 0)]
    [InlineData(5, 1.2)]
    public void Invalid_inputs_throw(int count, double p)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PopulationEstimator.Estimate(count, p));
    }
}

public class SurveyConditionsTests
{
    [Theory]
    [InlineData(2026, 2, Season.Wet)]
    [InlineData(2026, 6, Season.Dry)]
    [InlineData(2026, 10, Season.Dry)]
    [InlineData(2026, 11, Season.Wet)]
    public void Season_from_date(int y, int m, Season expected)
    {
        Assert.Equal(expected, SurveyConditions.SeasonFromDate(new DateOnly(y, m, 15)));
    }

    [Theory]
    [InlineData(5, 30, TimeWindow.EarlyMorning, true)]
    [InlineData(7, 0, TimeWindow.EarlyMorning, false)]
    [InlineData(9, 15, TimeWindow.MidMorning, true)]
    [InlineData(14, 0, TimeWindow.MidAfternoon, true)]
    [InlineData(16, 30, TimeWindow.EarlyEvening, false)]
    [InlineData(18, 45, TimeWindow.EarlyEvening, true)]
    public void Time_maps_to_nearest_study_window(int h, int m, TimeWindow expected, bool insideStudyHours)
    {
        var match = SurveyConditions.MatchTime(new TimeOnly(h, m));
        Assert.Equal(expected, match.Window);
        Assert.Equal(insideStudyHours, match.InsideStudyHours);
    }

    [Fact]
    public void Night_is_not_supported()
    {
        var match = SurveyConditions.MatchTime(new TimeOnly(22, 0));
        Assert.Null(match.Window);
        Assert.NotNull(match.Note);
    }
}

public class SurveyPlannerTests
{
    [Theory]
    [InlineData(Season.Wet, CameraType.Thermal)]
    [InlineData(Season.Dry, CameraType.Thermal)]
    [InlineData(Season.Wet, CameraType.Rgb)]
    [InlineData(Season.Dry, CameraType.Rgb)]
    public void Planner_recommends_early_evening_and_flags_afternoon(Season season, CameraType camera)
    {
        var plan = SurveyPlanner.Plan(season, camera);
        Assert.Equal(4, plan.Windows.Count);
        Assert.Equal(TimeWindow.EarlyEvening, plan.Windows.Single(w => w.IsBest).Window);
        Assert.Equal(TimeWindow.MidAfternoon, plan.Windows.Single(w => w.IsWorst).Window);
        Assert.Contains("early evening", plan.Recommendation);
    }
}
