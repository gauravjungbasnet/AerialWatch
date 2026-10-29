using Xunit;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Surveys.Models;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.Surveys;

/// <summary>Service layer tests using repository test doubles (no database needed).</summary>
public class SurveyServiceTests
{
    private readonly FixedClock _clock = new();
    private readonly FakeFileStorage _storage = new();
    private readonly SurveyService _service;

    public SurveyServiceTests()
    {
        var db = new FakeDatabase();
        _service = new SurveyService(new FakeSurveyRepository(db), db, _clock,
            new ActiveModelParameters(new FakeModelCoefficientSetRepository(db)), _storage);
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

        var list = await _service.ListAsync(new SurveyListQuery());
        Assert.Equal(new[] { "Second", "First" }, list.Items.Select(s => s.Name).ToArray());
        Assert.Equal(2, list.Total);
    }

    private async Task CreateMany(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
            await _service.CreateAsync(Valid(name: $"Survey {i:00}", date: $"2026-02-{i:00}") with
            {
                SiteName = i % 2 == 0 ? "Katherine" : "Darwin",
                Camera = i % 3 == 0 ? CameraType.Rgb : CameraType.Thermal,
            });
        }
    }

    [Fact]
    public async Task List_pages_on_the_server_and_reports_the_total()
    {
        await CreateMany(25);

        var page1 = await _service.ListAsync(new SurveyListQuery { PageSize = 10 });
        var page3 = await _service.ListAsync(new SurveyListQuery { PageSize = 10, Page = 3 });

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(25, page1.Total);
        Assert.Equal(5, page3.Items.Count);
        Assert.Equal("Survey 25", page1.Items[0].Name); // newest first by default
        Assert.Empty(page1.Items.Select(s => s.Id).Intersect(page3.Items.Select(s => s.Id)));
    }

    [Fact]
    public async Task List_filters_by_text_camera_date_and_status()
    {
        await CreateMany(12);

        var katherine = await _service.ListAsync(new SurveyListQuery { Search = "katHERine" }); // name or site, any case
        Assert.Equal(6, katherine.Total);

        var rgb = await _service.ListAsync(new SurveyListQuery { Camera = CameraType.Rgb });
        Assert.Equal(4, rgb.Total);
        Assert.All(rgb.Items, s => Assert.Equal(CameraType.Rgb, s.Camera));

        var oneDay = await _service.ListAsync(new SurveyListQuery { Date = "2026-02-07" });
        Assert.Equal("Survey 07", Assert.Single(oneDay.Items).Name);

        var combined = await _service.ListAsync(new SurveyListQuery { Search = "darwin", Camera = CameraType.Rgb });
        Assert.Equal(new[] { "Survey 09", "Survey 03" }, combined.Items.Select(s => s.Name).ToArray());

        var reviewed = await _service.ListAsync(new SurveyListQuery { Status = SurveyStatus.Reviewed });
        Assert.Equal(0, reviewed.Total);
    }

    [Fact]
    public async Task List_sorts_by_a_whitelisted_column_in_either_direction()
    {
        await CreateMany(5);

        var byNameAsc = await _service.ListAsync(new SurveyListQuery { Sort = "name" });
        var byDateDesc = await _service.ListAsync(new SurveyListQuery { Sort = "surveyDate", Dir = "desc" });

        Assert.Equal("Survey 01", byNameAsc.Items[0].Name);
        Assert.Equal("2026-02-05", byDateDesc.Items[0].SurveyDate);
    }

    [Fact]
    public async Task List_rejects_bad_paging_sort_and_date()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => _service.ListAsync(new SurveyListQuery
        {
            Page = 0, PageSize = 500, Sort = "password", Dir = "sideways", Date = "07/02/2026",
        }));
        foreach (var field in new[] { "page", "pageSize", "sort", "dir", "date" })
            Assert.True(ex.Errors.ContainsKey(field), $"Expected an error for {field}");
    }

    [Fact]
    public async Task Stats_count_surveys_awaiting_review_and_photos()
    {
        await CreateMany(3);
        var stats = await _service.GetStatsAsync();
        Assert.Equal(3, stats.Surveys);
        Assert.Equal(0, stats.AwaitingReview);
        Assert.Equal(0, stats.Photos);
    }

    [Fact]
    public async Task Delete_removes_and_missing_survey_is_not_found()
    {
        var created = await _service.CreateAsync(Valid());
        await _service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(created.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_also_removes_the_survey_photo_folder()
    {
        var created = await _service.CreateAsync(Valid());
        _storage.Files[$"wfp/surveys/{created.Id}/originals/x.jpg"] = new byte[] { 1 };

        await _service.DeleteAsync(created.Id);

        Assert.Equal($"wfp/surveys/{created.Id}", Assert.Single(_storage.DeletedFolders));
        Assert.Empty(_storage.Files);
    }
}
