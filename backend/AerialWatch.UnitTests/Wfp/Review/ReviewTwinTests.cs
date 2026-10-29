using Xunit;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Review.Models;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.Review;

/// <summary>Review with thermal + colour twins (D-039): only the survey's camera is reviewed; the twin comes with the photo.</summary>
public class ReviewTwinTests
{
    private readonly FakeDatabase _db = new();
    private readonly ReviewService _service;
    private readonly Survey _survey = new() { Name = "Pairs", SiteName = "Katherine", Camera = CameraType.Thermal, Status = SurveyStatus.Uploading };
    private readonly SurveyImage _thermal;
    private readonly SurveyImage _colour;

    public ReviewTwinTests()
    {
        _service = new ReviewService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db),
            new FakeDetectionRepository(_db), _db, new FixedClock());
        _thermal = new SurveyImage { SurveyId = _survey.Id, FileName = "X_THM.JPG", Camera = CameraType.Thermal, PairKey = "x", WidthPx = 640, HeightPx = 512 };
        _colour = new SurveyImage { SurveyId = _survey.Id, FileName = "X.JPG", Camera = CameraType.Rgb, PairKey = "x", WidthPx = 5472, HeightPx = 3648 };
        _db.Surveys.Add(_survey);
        _db.Images.AddRange(new[] { _thermal, _colour });
    }

    [Fact]
    public async Task Queue_and_progress_count_only_the_surveys_camera()
    {
        var summary = await _service.StartAsync(_survey.Id);
        Assert.Equal(1, summary.Photos);

        var queue = await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.All);
        Assert.Equal(_thermal.Id, Assert.Single(queue).Id);
    }

    [Fact]
    public async Task Photo_detail_includes_its_twin()
    {
        var detail = await _service.GetImageAsync(_thermal.Id);
        Assert.NotNull(detail.Twin);
        Assert.Equal(_colour.Id, detail.Twin!.Id);
        Assert.Equal(CameraType.Rgb, detail.Twin.Camera);
    }

    [Fact]
    public async Task Boxes_cannot_be_drawn_on_a_reference_photo()
    {
        await _service.StartAsync(_survey.Id);
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.AddDetectionAsync(_colour.Id, new AddDetectionRequest(10, 10, 20, 20), "ranger"));
        Assert.Contains("reference photo", ex.Errors.Values.SelectMany(v => v).First());
    }

    [Fact]
    public async Task Review_needs_a_photo_from_the_surveys_camera()
    {
        _db.Images.Remove(_thermal);
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => _service.StartAsync(_survey.Id));
        Assert.Contains("thermal", ex.Errors.Values.SelectMany(v => v).First());
    }
}
