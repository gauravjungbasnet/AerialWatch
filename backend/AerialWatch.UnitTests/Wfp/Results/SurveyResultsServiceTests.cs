using Xunit;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Results;
using AerialWatch.Contracts.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.Results;

/// <summary>Slice 5: only accepted boxes count; estimate = confirmed ÷ chance visible (Hvala et al. 2023).</summary>
public class SurveyResultsServiceTests
{
    private readonly FakeDatabase _db = new();
    private readonly SurveyResultsService _service;
    private readonly Survey _survey;
    private readonly SurveyImage _photo1;
    private readonly SurveyImage _photo2;

    public SurveyResultsServiceTests()
    {
        _service = new SurveyResultsService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db),
            new FakeDetectionRepository(_db), new ActiveModelParameters(new FakeModelCoefficientSetRepository(_db)));

        // Same conditions as Estimate_service_matches_research_model: thermal, wet season, 18:20 (early evening), 5 km².
        _survey = new Survey
        {
            Name = "North paddock", SiteName = "Katherine", Camera = CameraType.Thermal, Season = Season.Wet,
            SurveyDate = new DateOnly(2026, 2, 10), StartTime = new TimeOnly(18, 20), TimeWindow = TimeWindow.EarlyEvening,
            AreaKm2 = 5, Status = SurveyStatus.InReview,
        };
        _photo1 = Photo("DJI_0001.JPG", lat: -14.47, lon: 132.3, reviewed: true);
        _photo2 = Photo("DJI_0002.JPG", lat: null, lon: null, reviewed: false);
        _db.Surveys.Add(_survey);
        _db.Images.AddRange(new[] { _photo1, _photo2 });
    }

    private SurveyImage Photo(string name, double? lat, double? lon, bool reviewed) => new()
    {
        SurveyId = _survey.Id, FileName = name, WidthPx = 640, HeightPx = 512, Latitude = lat, Longitude = lon, Reviewed = reviewed,
    };

    private void Box(SurveyImage photo, DetectionSource source, DetectionStatus status, int count = 1)
    {
        for (var i = 0; i < count; i++)
            _db.Detections.Add(new AnimalDetection
            {
                ImageId = photo.Id, X = 10 + i, Y = 10, Width = 20, Height = 15, Source = source, Status = status,
                Confidence = source == DetectionSource.Ai ? 0.8 : null,
            });
    }

    [Fact]
    public async Task Only_accepted_boxes_are_counted_and_split_by_source()
    {
        Box(_photo1, DetectionSource.Ai, DetectionStatus.Accepted, 10);
        Box(_photo1, DetectionSource.Ranger, DetectionStatus.Accepted, 5);
        Box(_photo1, DetectionSource.Ai, DetectionStatus.Rejected, 3);
        Box(_photo2, DetectionSource.Ai, DetectionStatus.Pending, 2);

        var r = await _service.GetAsync(_survey.Id);

        Assert.Equal(15, r.Counts.Confirmed);
        Assert.Equal(10, r.Counts.ConfirmedFromAi);
        Assert.Equal(5, r.Counts.DrawnByRanger);
        Assert.Equal(3, r.Counts.RejectedAi);
        Assert.Equal(2, r.Counts.PendingAi);
        Assert.Equal(1, r.Counts.PhotosWithPigs);
        Assert.Equal(1, r.Counts.PhotosWithGps);
        Assert.Equal(15, r.Photos.Single(p => p.ImageId == _photo1.Id).Confirmed);
    }

    [Fact]
    public async Task Estimate_matches_the_research_model_and_is_provisional_during_review()
    {
        Box(_photo1, DetectionSource.Ai, DetectionStatus.Accepted, 15);

        var r = await _service.GetAsync(_survey.Id);

        Assert.True(r.Provisional);
        Assert.Contains("1 of 2 photos checked", r.ProvisionalReason);
        Assert.NotNull(r.Estimate);
        Assert.Equal(21, r.Estimate!.EstimateRounded);
        Assert.Equal(4.16, r.Estimate.DensityPerKm2);
        Assert.True(r.Estimate.Provisional);
        Assert.True(r.Estimate.Low <= 21 && r.Estimate.High >= 21);
    }

    [Fact]
    public async Task Completed_review_is_not_provisional()
    {
        _survey.Status = SurveyStatus.Reviewed;
        Box(_photo1, DetectionSource.Ranger, DetectionStatus.Accepted, 4);

        var r = await _service.GetAsync(_survey.Id);

        Assert.False(r.Provisional);
        Assert.Null(r.ProvisionalReason);
        Assert.False(r.Estimate!.Provisional);
    }

    [Fact]
    public async Task Night_flight_has_counts_but_no_estimate()
    {
        _survey.StartTime = new TimeOnly(23, 0);
        _survey.TimeWindow = null;
        Box(_photo1, DetectionSource.Ranger, DetectionStatus.Accepted, 2);

        var r = await _service.GetAsync(_survey.Id);

        Assert.Equal(2, r.Counts.Confirmed);
        Assert.Null(r.Estimate);
        Assert.False(string.IsNullOrWhiteSpace(r.EstimateUnavailableReason));
    }

    [Fact]
    public async Task Unknown_survey_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(Guid.NewGuid()));
    }
}
