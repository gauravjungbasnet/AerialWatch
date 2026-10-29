using Xunit;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Review.Models;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.Review;

/// <summary>Review rules from D-034, with in-memory repositories (no database).</summary>
public class ReviewServiceTests
{
    private const string Ranger = "ranger.jo";

    private readonly FakeDatabase _db = new();
    private readonly FixedClock _clock = new();
    private readonly ReviewService _service;
    private readonly Survey _survey;
    private readonly SurveyImage _photo1;
    private readonly SurveyImage _photo2;

    public ReviewServiceTests()
    {
        _service = new ReviewService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db),
            new FakeDetectionRepository(_db), _db, _clock);

        _survey = new Survey { Name = "North paddock", SiteName = "Katherine", Camera = CameraType.Thermal, Status = SurveyStatus.Uploading };
        _photo1 = Photo("DJI_0001.JPG", minute: 1);
        _photo2 = Photo("DJI_0002.JPG", minute: 2);
        _db.Surveys.Add(_survey);
        _db.Images.AddRange(new[] { _photo1, _photo2 });
    }

    private SurveyImage Photo(string name, int minute) => new()
    {
        SurveyId = _survey.Id,
        FileName = name,
        WidthPx = 640,
        HeightPx = 512,
        TakenAt = new DateTimeOffset(2026, 2, 10, 18, minute, 0, TimeSpan.FromHours(9.5)),
        ThumbnailPath = "thumb",
    };

    private AnimalDetection AiBox(SurveyImage photo, double confidence, DetectionStatus status = DetectionStatus.Pending)
    {
        var d = new AnimalDetection
        {
            ImageId = photo.Id, X = 10, Y = 10, Width = 20, Height = 15, Confidence = confidence,
            Source = DetectionSource.Ai, Status = status, ModelVersion = "yolov8n-bambi-0.1",
        };
        _db.Detections.Add(d);
        return d;
    }

    private async Task StartReview() => await _service.StartAsync(_survey.Id);

    private static async Task<string> Fails(Func<Task> act)
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(act);
        return ex.Errors.Values.SelectMany(v => v).First();
    }

    // ---- starting and finishing ----

    [Fact]
    public async Task Start_moves_an_uploaded_survey_into_review()
    {
        var summary = await _service.StartAsync(_survey.Id);

        Assert.Equal(SurveyStatus.InReview, _survey.Status);
        Assert.Equal(SurveyStatus.InReview, summary.Status);
        Assert.Equal(2, summary.Photos);
        Assert.Equal(0, summary.ReviewedPhotos);
        Assert.False(summary.CanComplete);
        Assert.Equal("2 photos are not checked yet.", summary.BlockedReason);
    }

    [Fact]
    public async Task Start_twice_is_harmless()
    {
        await StartReview();
        var saves = _db.SaveCalls;
        var summary = await _service.StartAsync(_survey.Id);
        Assert.Equal(SurveyStatus.InReview, summary.Status);
        Assert.Equal(saves, _db.SaveCalls);
    }

    [Fact]
    public async Task Start_needs_photos()
    {
        _survey.Status = SurveyStatus.Draft;
        Assert.Equal("Upload photos before starting the review.", await Fails(StartReview));
    }

    [Fact]
    public async Task Start_from_processing_is_allowed_once_the_AI_has_run()
    {
        _survey.Status = SurveyStatus.Processing;
        await StartReview();
        Assert.Equal(SurveyStatus.InReview, _survey.Status);
    }

    [Fact]
    public async Task Changes_are_refused_before_the_review_starts()
    {
        var message = await Fails(() => _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), Ranger));
        Assert.Equal("Start the review first.", message);
        Assert.Empty(_db.Detections);
    }

    [Fact]
    public async Task Complete_survey_needs_every_photo_checked_and_nothing_pending()
    {
        await StartReview();
        AiBox(_photo1, 0.9);

        Assert.Contains("1 AI suggestion is still waiting", await Fails(() => _service.CompleteSurveyAsync(_survey.Id)));

        await _service.UpdateDetectionAsync(_db.Detections[0].Id, new UpdateDetectionRequest(Status: DetectionStatus.Accepted), Ranger);
        await _service.CompleteImageAsync(_photo1.Id, Ranger);
        Assert.Equal("1 photo is not checked yet.", await Fails(() => _service.CompleteSurveyAsync(_survey.Id)));

        await _service.CompleteImageAsync(_photo2.Id, Ranger);
        var summary = await _service.CompleteSurveyAsync(_survey.Id);

        Assert.Equal(SurveyStatus.Reviewed, _survey.Status);
        Assert.Equal(SurveyStatus.Reviewed, summary.Status);
        Assert.Equal(1, summary.ConfirmedPigs);
        Assert.Equal(2, summary.ReviewedPhotos);
    }

    [Fact]
    public async Task A_completed_review_is_read_only_until_reopened()
    {
        await StartReview();
        await _service.CompleteImageAsync(_photo1.Id, Ranger);
        await _service.CompleteImageAsync(_photo2.Id, Ranger);
        await _service.CompleteSurveyAsync(_survey.Id);

        var message = await Fails(() => _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), Ranger));
        Assert.Equal("This review is complete. Reopen it to make changes.", message);

        await _service.ReopenSurveyAsync(_survey.Id);
        Assert.Equal(SurveyStatus.InReview, _survey.Status);
        await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), Ranger);
        Assert.Single(_db.Detections);
    }

    [Fact]
    public async Task Only_a_completed_review_can_be_reopened()
    {
        await StartReview();
        Assert.Equal("Only a completed review can be reopened.", await Fails(() => _service.ReopenSurveyAsync(_survey.Id)));
    }

    // ---- boxes ----

    [Fact]
    public async Task A_box_drawn_by_a_ranger_is_a_confirmed_pig_with_an_audit_trail()
    {
        await StartReview();
        var box = await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(100, 50, 24, 18), Ranger);

        Assert.Equal(DetectionSource.Ranger, box.Source);
        Assert.Equal(DetectionStatus.Accepted, box.Status);
        Assert.Null(box.Confidence);
        Assert.Equal("pig", box.Label);
        Assert.Equal(Ranger, box.ReviewedBy);
        Assert.Equal(_clock.UtcNow, box.ReviewedAt);

        var summary = await _service.GetSummaryAsync(_survey.Id);
        Assert.Equal(1, summary.ConfirmedPigs);
        Assert.Equal(1, summary.RangerAdded);
        Assert.Equal(1, summary.PhotosWithPigs);
    }

    [Theory]
    [InlineData(0, 0, 3, 10, "at least 4")]          // too small: a click, not a drag
    [InlineData(-5, 0, 10, 10, "start inside")]
    [InlineData(630, 0, 20, 10, "right edge")]       // photo is 640 wide
    [InlineData(0, 500, 10, 20, "bottom edge")]      // photo is 512 high
    [InlineData(double.NaN, 0, 10, 10, "number")]
    public async Task Boxes_must_fit_inside_the_photo(double x, double y, double w, double h, string expected)
    {
        await StartReview();
        Assert.Contains(expected, await Fails(() => _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(x, y, w, h), Ranger)));
        Assert.Empty(_db.Detections);
    }

    [Fact]
    public async Task A_box_may_touch_the_edge_within_rounding()
    {
        await StartReview();
        await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(620.4, 492.2, 20.4, 20.5), Ranger);
        Assert.Single(_db.Detections);
    }

    [Fact]
    public async Task Accept_and_reject_record_who_and_when_and_undo_clears_it()
    {
        await StartReview();
        var ai = AiBox(_photo1, 0.42);

        var accepted = await _service.UpdateDetectionAsync(ai.Id, new UpdateDetectionRequest(Status: DetectionStatus.Accepted), Ranger);
        Assert.Equal(DetectionStatus.Accepted, accepted.Status);
        Assert.Equal(Ranger, accepted.ReviewedBy);

        var rejected = await _service.UpdateDetectionAsync(ai.Id, new UpdateDetectionRequest(Status: DetectionStatus.Rejected), "ranger.sam");
        Assert.Equal(DetectionStatus.Rejected, rejected.Status);
        Assert.Equal("ranger.sam", rejected.ReviewedBy);

        var undone = await _service.UpdateDetectionAsync(ai.Id, new UpdateDetectionRequest(Status: DetectionStatus.Pending), Ranger);
        Assert.Equal(DetectionStatus.Pending, undone.Status);
        Assert.Null(undone.ReviewedBy);
        Assert.Null(undone.ReviewedAt);
    }

    [Fact]
    public async Task Moving_a_box_needs_all_four_values_and_stays_inside_the_photo()
    {
        await StartReview();
        var ai = AiBox(_photo1, 0.8);

        Assert.Contains("together", await Fails(() => _service.UpdateDetectionAsync(ai.Id, new UpdateDetectionRequest(X: 5), Ranger)));
        Assert.Contains("right edge", await Fails(() => _service.UpdateDetectionAsync(ai.Id, new UpdateDetectionRequest(630, 0, 20, 20), Ranger)));

        var moved = await _service.UpdateDetectionAsync(ai.Id, new UpdateDetectionRequest(50, 60, 30, 25), Ranger);
        Assert.Equal((50d, 60d, 30d, 25d), (moved.X, moved.Y, moved.Width, moved.Height));
        Assert.Equal(DetectionStatus.Pending, moved.Status); // moving isn't accepting
        Assert.Equal(Ranger, moved.ReviewedBy);
    }

    [Fact]
    public async Task Ranger_boxes_cannot_go_back_to_pending()
    {
        await StartReview();
        var box = await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), Ranger);
        Assert.Contains("delete the box", await Fails(() => _service.UpdateDetectionAsync(box.Id, new UpdateDetectionRequest(Status: DetectionStatus.Pending), Ranger)));
    }

    [Fact]
    public async Task Ranger_boxes_can_be_deleted_but_AI_suggestions_are_only_rejected()
    {
        await StartReview();
        var mine = await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), Ranger);
        var ai = AiBox(_photo1, 0.7);

        await _service.DeleteDetectionAsync(mine.Id);
        Assert.DoesNotContain(_db.Detections, d => d.Id == mine.Id);

        Assert.Contains("reject it instead", await Fails(() => _service.DeleteDetectionAsync(ai.Id)));
        Assert.Contains(_db.Detections, d => d.Id == ai.Id);
    }

    [Fact]
    public async Task Unknown_box_is_not_found()
    {
        await StartReview();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateDetectionAsync(Guid.NewGuid(), new UpdateDetectionRequest(Status: DetectionStatus.Accepted), Ranger));
    }

    // ---- photos ----

    [Fact]
    public async Task A_photo_with_pending_suggestions_cannot_be_marked_checked()
    {
        await StartReview();
        AiBox(_photo1, 0.6);
        AiBox(_photo1, 0.5);

        Assert.Equal("Accept or reject the 2 suggested boxes on this photo first.", await Fails(() => _service.CompleteImageAsync(_photo1.Id, Ranger)));
        Assert.False(_photo1.Reviewed);
    }

    [Fact]
    public async Task A_photo_with_no_pigs_can_be_checked_in_one_step_and_reopened()
    {
        await StartReview();
        var dto = await _service.CompleteImageAsync(_photo2.Id, Ranger);

        Assert.True(dto.Reviewed);
        Assert.Equal(0, dto.Confirmed);
        Assert.Equal(Ranger, _photo2.ReviewedBy);
        Assert.Equal(_clock.UtcNow, _photo2.ReviewedAt);

        var reopened = await _service.ReopenImageAsync(_photo2.Id);
        Assert.False(reopened.Reviewed);
        Assert.Null(_photo2.ReviewedBy);
    }

    [Fact]
    public async Task Flagging_marks_a_photo_for_a_second_look_without_blocking_completion()
    {
        await StartReview();
        await _service.FlagImageAsync(_photo1.Id, new FlagImageRequest(true));
        await _service.CompleteImageAsync(_photo1.Id, Ranger);
        await _service.CompleteImageAsync(_photo2.Id, Ranger);

        var summary = await _service.GetSummaryAsync(_survey.Id);
        Assert.Equal(1, summary.FlaggedPhotos);
        Assert.True(summary.CanComplete);
    }

    [Fact]
    public async Task Queue_filters_and_counts_per_photo()
    {
        await StartReview();
        AiBox(_photo1, 0.9, DetectionStatus.Accepted);
        AiBox(_photo1, 0.3, DetectionStatus.Rejected);
        AiBox(_photo1, 0.5);
        await _service.CompleteImageAsync(_photo2.Id, Ranger);
        await _service.FlagImageAsync(_photo2.Id, new FlagImageRequest(true));

        var all = await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.All);
        Assert.Equal(new[] { "DJI_0001.JPG", "DJI_0002.JPG" }, all.Select(i => i.FileName)); // flight order
        var first = all[0];
        Assert.Equal((1, 1, 1), (first.Confirmed, first.Pending, first.Rejected));

        Assert.Equal("DJI_0001.JPG", Assert.Single(await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.NotReviewed)).FileName);
        Assert.Equal("DJI_0001.JPG", Assert.Single(await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.Pending)).FileName);
        Assert.Equal("DJI_0001.JPG", Assert.Single(await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.WithPigs)).FileName);
        Assert.Equal("DJI_0002.JPG", Assert.Single(await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.NoPigs)).FileName);
        Assert.Equal("DJI_0002.JPG", Assert.Single(await _service.ListImagesAsync(_survey.Id, ReviewImageFilter.Flagged)).FileName);
    }

    [Fact]
    public async Task Image_detail_lists_AI_suggestions_first_by_confidence()
    {
        await StartReview();
        await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), Ranger);
        AiBox(_photo1, 0.4);
        AiBox(_photo1, 0.95);

        var detail = await _service.GetImageAsync(_photo1.Id);
        Assert.Equal(new double?[] { 0.95, 0.4, null }, detail.Detections.Select(d => d.Confidence));
        Assert.Equal((1, 2), (detail.Image.Confirmed, detail.Image.Pending));
    }

    // ---- bulk accept ----

    [Fact]
    public async Task Bulk_accept_takes_only_pending_AI_suggestions_at_or_above_the_threshold()
    {
        await StartReview();
        var high = AiBox(_photo1, 0.91);
        var exact = AiBox(_photo2, 0.8);
        var low = AiBox(_photo2, 0.79);
        var rejected = AiBox(_photo1, 0.99, DetectionStatus.Rejected);

        var result = await _service.BulkAcceptAsync(_survey.Id, new BulkAcceptRequest(0.8), Ranger);

        Assert.Equal(2, result.Accepted);
        Assert.Equal(DetectionStatus.Accepted, high.Status);
        Assert.Equal(DetectionStatus.Accepted, exact.Status);
        Assert.Equal(DetectionStatus.Pending, low.Status);
        Assert.Equal(DetectionStatus.Rejected, rejected.Status);   // a ranger's decision is never overwritten
        Assert.Equal(Ranger, high.ReviewedBy);
        Assert.Equal(2, result.Summary.ConfirmedPigs);
        Assert.Equal(1, result.Summary.PendingSuggestions);
        Assert.Equal(4, result.Summary.AiSuggestions);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public async Task Bulk_accept_threshold_must_be_a_probability(double min)
    {
        await StartReview();
        Assert.Contains("between 0 and 1", await Fails(() => _service.BulkAcceptAsync(_survey.Id, new BulkAcceptRequest(min), Ranger)));
    }

    [Fact]
    public async Task Long_user_names_are_trimmed_to_the_column()
    {
        await StartReview();
        var box = await _service.AddDetectionAsync(_photo1.Id, new AddDetectionRequest(1, 1, 10, 10), new string('x', 150));
        Assert.Equal(AnimalDetection.ReviewedByMaxLength, box.ReviewedBy!.Length);
    }
}
