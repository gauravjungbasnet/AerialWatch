using Xunit;
using AerialWatch.Application.Wfp.TwinCheck;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.TwinCheck.Models;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.TwinCheck;

/// <summary>
/// Twin check (D-040). Thermal 640×512 with a colour twin 6400×5120 (10× each way), so with no marks a box maps to
/// ten times its position and size.
/// </summary>
public class TwinCheckServiceTests
{
    private readonly FakeDatabase _db = new();
    private readonly TwinCheckService _service;
    private readonly Survey _survey = new() { Name = "Pairs", SiteName = "Katherine", Camera = CameraType.Thermal, Status = SurveyStatus.InReview };

    public TwinCheckServiceTests()
    {
        _service = new TwinCheckService(new FakeSurveyRepository(_db), new FakeSurveyImageRepository(_db), new FakeDetectionRepository(_db),
            new FakeTwinMarkRepository(_db), _db, new FixedClock());
        _db.Surveys.Add(_survey);
    }

    private (SurveyImage Thermal, SurveyImage Colour) Pair(string key)
    {
        var t = new SurveyImage { SurveyId = _survey.Id, FileName = $"{key}_THM.JPG", Camera = CameraType.Thermal, PairKey = key, WidthPx = 640, HeightPx = 512 };
        var c = new SurveyImage { SurveyId = _survey.Id, FileName = $"{key}.JPG", Camera = CameraType.Rgb, PairKey = key, WidthPx = 6400, HeightPx = 5120 };
        _db.Images.AddRange(new[] { t, c });
        return (t, c);
    }

    private AnimalDetection Box(SurveyImage photo, double x, double y, double size = 20, DetectionStatus status = DetectionStatus.Accepted)
    {
        var d = new AnimalDetection { ImageId = photo.Id, X = x, Y = y, Width = size, Height = size, Source = DetectionSource.Ranger, Status = status };
        _db.Detections.Add(d);
        return d;
    }

    [Fact]
    public async Task Without_marks_the_suggestion_scales_by_photo_size()
    {
        var (thermal, colour) = Pair("a");
        Box(thermal, 100, 100);

        var check = await _service.GetAsync(thermal.Id);

        Assert.Equal(colour.Id, check.TwinImageId);
        var mark = Assert.Single(check.Marks);
        Assert.Equal(TwinMarkState.Suggested, mark.State);
        Assert.Equal((1000d, 1000d, 200d, 200d), (mark.X, mark.Y, mark.Width, mark.Height));
        Assert.Equal(1, mark.Number);
    }

    [Fact]
    public async Task A_placed_mark_moves_the_next_suggestions()
    {
        var (t1, _) = Pair("a");
        var (t2, _) = Pair("b");
        var first = Box(t1, 100, 100);           // centre (110,110) → plain scaling says (1100,1100)
        var second = Box(t2, 300, 200, size: 10); // centre (305,205)

        // The ranger finds the first pig 100 px right and 50 px down from the guess.
        await _service.SetAsync(first.Id, new SetTwinMarkRequest(TwinMarkState.Confirmed, 1100, 1050, 200, 200), "ranger");

        var check = await _service.GetAsync(t2.Id);
        var mark = Assert.Single(check.Marks);
        Assert.Equal(1, check.LearnedFrom);
        Assert.Equal((3100d, 2050d, 100d, 100d), (mark.X, mark.Y, mark.Width, mark.Height)); // centre 10×305+100, 10×205+50
        Assert.Equal(TwinMarkState.Confirmed, (await _service.GetAsync(t1.Id)).Marks.Single().State);
    }

    [Fact]
    public void Two_or_more_marks_fit_scale_and_offset_per_axis()
    {
        // Twin = 8 × source + 500 on x, 9 × source − 100 on y.
        static MatchedBoxes P(double sx, double sy) =>
            new(new PixelBox(sx - 5, sy - 5, 10, 10), new PixelBox(8 * sx + 500 - 40, 9 * sy - 100 - 45, 80, 90));
        var mapping = TwinMapping.Fit(new[] { P(100, 100), P(400, 300), P(250, 450) }, 10, 10);

        Assert.Equal(8, mapping.ScaleX, 6);
        Assert.Equal(500, mapping.OffsetX, 6);
        Assert.Equal(9, mapping.ScaleY, 6);
        Assert.Equal(-100, mapping.OffsetY, 6);
        var mapped = mapping.Map(new PixelBox(195, 195, 10, 10), 6400, 5120); // centre (200,200) → (2100,1700)
        Assert.Equal(2100, mapped.CentreX, 6);
        Assert.Equal(1700, mapped.CentreY, 6);
    }

    [Fact]
    public async Task Not_visible_is_recorded_and_counted_in_the_summary()
    {
        var (t1, _) = Pair("a");
        var seen = Box(t1, 100, 100);
        var hidden = Box(t1, 300, 300);
        Box(t1, 500, 400);                                                        // not checked yet

        await _service.SetAsync(seen.Id, new SetTwinMarkRequest(TwinMarkState.Confirmed, 1000, 1000, 200, 200), "ranger");
        await _service.SetAsync(hidden.Id, new SetTwinMarkRequest(TwinMarkState.NotVisible), "ranger");

        var summary = await _service.GetSummaryAsync(_survey.Id);
        Assert.Equal(CameraType.Rgb, summary.TwinCamera);
        Assert.Equal(3, summary.PigsWithTwin);
        Assert.Equal(2, summary.Checked);
        Assert.Equal(1, summary.VisibleOnTwin);
        Assert.Equal(1, summary.NotVisibleOnTwin);
        Assert.Equal(0, summary.SeenOnlyOnTwin);
        Assert.Null(_db.TwinMarks.Single(m => m.DetectionId == hidden.Id).X);

        await _service.ClearAsync(hidden.Id);
        Assert.Equal(TwinMarkState.Suggested, (await _service.GetAsync(t1.Id)).Marks.Single(m => m.DetectionId == hidden.Id).State);
    }

    [Fact]
    public async Task Only_confirmed_pigs_on_open_reviews_can_be_checked()
    {
        var (t1, _) = Pair("a");
        var pending = Box(t1, 100, 100, status: DetectionStatus.Pending);
        var accepted = Box(t1, 300, 300);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.SetAsync(pending.Id, new SetTwinMarkRequest(TwinMarkState.NotVisible), "ranger"));
        Assert.DoesNotContain((await _service.GetAsync(t1.Id)).Marks, m => m.DetectionId == pending.Id);

        await Assert.ThrowsAsync<RequestValidationException>(() =>   // box outside the 6400×5120 twin
            _service.SetAsync(accepted.Id, new SetTwinMarkRequest(TwinMarkState.Confirmed, 6300, 100, 200, 200), "ranger"));

        _survey.Status = SurveyStatus.Reviewed;
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.SetAsync(accepted.Id, new SetTwinMarkRequest(TwinMarkState.NotVisible), "ranger"));
    }

    [Fact]
    public async Task Photo_without_twin_has_nothing_to_check()
    {
        var single = new SurveyImage { SurveyId = _survey.Id, FileName = "solo_THM.JPG", Camera = CameraType.Thermal, PairKey = "solo", WidthPx = 640, HeightPx = 512 };
        _db.Images.Add(single);
        Box(single, 10, 10);

        var check = await _service.GetAsync(single.Id);
        Assert.Null(check.TwinImageId);
        Assert.Empty(check.Marks);
        Assert.Null((await _service.GetSummaryAsync(_survey.Id)).TwinCamera);
    }

    // ---- the other way: drawn first on the twin (D-041) ----

    [Fact]
    public async Task A_pig_drawn_on_the_twin_is_suggested_on_the_counted_photo_and_confirming_counts_it()
    {
        var (thermal, _) = Pair("a");

        var sighting = await _service.AddSightingAsync(thermal.Id, new TwinBoxRequest(1000, 2000, 200, 100), "ranger");

        Assert.Equal("c1", sighting.Label);
        Assert.Equal(TwinSightingState.SuggestedOnCounted, sighting.State);
        Assert.Equal((100d, 200d, 20d, 10d), (sighting.X, sighting.Y, sighting.Width, sighting.Height)); // ÷10, no marks yet
        Assert.Empty(_db.Detections);                                    // not counted until confirmed

        var mark = await _service.ConfirmSightingAsync(sighting.MarkId, new TwinBoxRequest(102, 198, 20, 12), "ranger");

        var pig = Assert.Single(_db.Detections);
        Assert.Equal((DetectionSource.Ranger, DetectionStatus.Accepted, thermal.Id), (pig.Source, pig.Status, pig.ImageId));
        Assert.Equal(pig.Id, mark.DetectionId);
        Assert.Equal(TwinMarkState.Confirmed, mark.State);
        var check = await _service.GetAsync(thermal.Id);
        Assert.Empty(check.Sightings);
        Assert.Equal((1000d, 2000d), (check.Marks.Single().X, check.Marks.Single().Y));  // keeps where it was drawn on colour
    }

    [Fact]
    public async Task Placed_pairs_improve_suggestions_in_the_reverse_direction_too()
    {
        var (t1, _) = Pair("a");
        var (t2, _) = Pair("b");
        var first = Box(t1, 100, 100);           // centre (110,110)
        await _service.SetAsync(first.Id, new SetTwinMarkRequest(TwinMarkState.Confirmed, 1100, 1050, 200, 200), "ranger"); // +100, +50

        var sighting = await _service.AddSightingAsync(t2.Id, new TwinBoxRequest(3100, 2050, 100, 100), "ranger"); // centre (3150,2100)

        Assert.Equal((300d, 200d, 10d, 10d), (sighting.X, sighting.Y, sighting.Width, sighting.Height)); // (3150−100)/10, (2100−50)/10
    }

    [Fact]
    public async Task A_sighting_not_visible_on_the_counted_photo_is_never_counted_but_is_summarised()
    {
        var (thermal, _) = Pair("a");
        Box(thermal, 100, 100);
        var sighting = await _service.AddSightingAsync(thermal.Id, new TwinBoxRequest(4000, 4000, 100, 100), "ranger");

        var hidden = await _service.SetSightingVisibilityAsync(sighting.MarkId, new SetSightingVisibilityRequest(true), "ranger");

        Assert.Equal(TwinSightingState.NotVisibleOnCounted, hidden.State);
        Assert.Single(_db.Detections);                                   // still only the thermal pig
        Assert.Equal(1, (await _service.GetSummaryAsync(_survey.Id)).SeenOnlyOnTwin);

        await _service.RemoveSightingAsync(sighting.MarkId);
        Assert.Empty((await _service.GetAsync(thermal.Id)).Sightings);
    }

    [Fact]
    public async Task Sightings_need_an_open_review_a_twin_and_a_box_inside_the_photo()
    {
        var (thermal, _) = Pair("a");
        await Assert.ThrowsAsync<RequestValidationException>(() =>        // outside the 6400×5120 twin
            _service.AddSightingAsync(thermal.Id, new TwinBoxRequest(6390, 10, 50, 50), "ranger"));

        var sighting = await _service.AddSightingAsync(thermal.Id, new TwinBoxRequest(10, 10, 50, 50), "ranger");
        await Assert.ThrowsAsync<RequestValidationException>(() =>        // outside the 640×512 counted photo
            _service.ConfirmSightingAsync(sighting.MarkId, new TwinBoxRequest(630, 10, 20, 20), "ranger"));

        var solo = new SurveyImage { SurveyId = _survey.Id, FileName = "solo_THM.JPG", Camera = CameraType.Thermal, PairKey = "solo", WidthPx = 640, HeightPx = 512 };
        _db.Images.Add(solo);
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.AddSightingAsync(solo.Id, new TwinBoxRequest(10, 10, 50, 50), "ranger"));

        _survey.Status = SurveyStatus.Reviewed;
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.AddSightingAsync(thermal.Id, new TwinBoxRequest(10, 10, 50, 50), "ranger"));
    }
}
