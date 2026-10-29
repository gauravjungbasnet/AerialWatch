using Xunit;
using AerialWatch.Application.Common.Training;
using AerialWatch.Application.Wfp.Training;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Common.Training;
using AerialWatch.Contracts.Wfp.Training.Models;
using AerialWatch.Domain.Common.Training;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;
using AerialWatch.UnitTests.TestDoubles;

namespace AerialWatch.UnitTests.Wfp.Training;

/// <summary>AI training data (D-038): tagging, counts, warnings and the YOLO export rules R1–R8. Photos are 640 × 512.</summary>
public class TrainingDataServiceTests
{
    private readonly FakeDatabase _db = new();
    private readonly FixedClock _clock = new();   // 2026-10-19 09:00:00 UTC
    private readonly FakeDatasetWriter _writer = new();
    private readonly TrainingDataService _service;

    public TrainingDataServiceTests()
    {
        _service = new TrainingDataService(new FakeSurveyRepository(_db), new FakeTrainingDataRepository(_db),
            new TrainingDatasetExporter(_writer, _clock), _db, _clock);
    }

    // ---- set split ----

    [Fact]
    public async Task SetSplit_saves_and_clears_the_set()
    {
        var survey = AddSurvey("North", split: null);

        var tagged = await _service.SetSplitAsync(survey.Id, new SetTrainingSplitRequest(TrainingSplit.Test));
        Assert.Equal(TrainingSplit.Test, tagged.Split);
        Assert.Equal(TrainingSplit.Test, survey.TrainingSplit);

        var cleared = await _service.SetSplitAsync(survey.Id, new SetTrainingSplitRequest(null));
        Assert.Null(cleared.Split);
        Assert.Null(survey.TrainingSplit);
        Assert.Equal(2, _db.SaveCalls);
    }

    [Fact]
    public async Task SetSplit_for_an_unknown_survey_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SetSplitAsync(Guid.NewGuid(), new SetTrainingSplitRequest(TrainingSplit.Train)));

    // ---- overview ----

    [Fact]
    public async Task Overview_counts_only_checked_unflagged_photos_and_accepted_pig_boxes()
    {
        var s = AddSurvey("Train A", TrainingSplit.Train);
        var withPigs = AddImage(s, "a.jpg");
        AddBox(withPigs);
        AddBox(withPigs);
        AddBox(withPigs, DetectionStatus.Rejected);       // R3: rejected AI box doesn't count
        AddImage(s, "b.jpg");                              // checked, no pigs: usable "nothing here" photo
        AddBox(AddImage(s, "c.jpg", flagged: true));       // R2: flagged photo left out
        AddBox(AddImage(s, "d.jpg", reviewed: false));     // R2: unchecked photo left out

        var overview = await _service.GetOverviewAsync(CameraType.Thermal);

        var row = Assert.Single(overview.Surveys);
        Assert.Equal((4, 3, 1, 2, 1, 2), (row.Photos, row.CheckedPhotos, row.FlaggedPhotos, row.UsablePhotos, row.PhotosWithPigs, row.Pigs));
        var train = overview.Totals.Single(t => t.Split == TrainingSplit.Train);
        Assert.Equal((1, 2, 1, 2), (train.Sessions, train.Photos, train.PhotosWithLabels, train.Labels));
        Assert.False(overview.CanExport);                                       // R6: no validation or test yet
        Assert.Contains(overview.Warnings, w => w.Contains("Validation"));
        Assert.Contains(overview.Warnings, w => w.Contains("not checked"));
        Assert.Equal(_writer.ExportRoot, overview.ExportRoot);
    }

    [Fact]
    public async Task Overview_shows_one_camera_and_is_exportable_when_every_set_has_a_photo()
    {
        AddImage(AddSurvey("Train", TrainingSplit.Train), "a.jpg");
        AddImage(AddSurvey("Val", TrainingSplit.Validation), "b.jpg");
        AddImage(AddSurvey("Test", TrainingSplit.Test), "c.jpg");
        AddImage(AddSurvey("Colour", TrainingSplit.Test, CameraType.Rgb), "d.jpg");
        AddSurvey("Not tagged", split: null);

        var thermal = await _service.GetOverviewAsync(CameraType.Thermal);

        Assert.Equal(new[] { "Train", "Val", "Test" }.Order(), thermal.Surveys.Select(s => s.Name).Order());
        Assert.True(thermal.CanExport);
    }

    // ---- export ----

    [Fact]
    public async Task Reference_twins_from_the_other_camera_are_not_counted_or_exported()
    {
        var train = AddSurvey("Train", TrainingSplit.Train);
        AddImage(AddSurvey("Val", TrainingSplit.Validation), "b.jpg");
        AddImage(AddSurvey("Test", TrainingSplit.Test), "c.jpg");
        AddImage(train, "x_THM.JPG");
        var twin = AddImage(train, "x.JPG");
        twin.Camera = CameraType.Rgb;                       // colour reference in a thermal survey (D-039)

        var overview = await _service.GetOverviewAsync(CameraType.Thermal);
        Assert.Equal(1, overview.Surveys.Single(s => s.Name == "Train").Photos);

        await _service.ExportAsync(new ExportTrainingDataRequest(CameraType.Thermal), "gaurav");
        Assert.DoesNotContain(twin.StoragePath, _writer.Images.Values);
    }

    [Fact]
    public async Task Export_writes_a_yolo_dataset_split_by_survey()
    {
        var train = AddSurvey("Train A", TrainingSplit.Train);
        var pigPhoto = AddImage(train, "a.jpg");
        AddBox(pigPhoto);                                  // 64,128 32×64 → 0 0.125000 0.312500 0.050000 0.125000
        AddBox(AddImage(train, "flagged.jpg", flagged: true));
        var emptyPhoto = AddImage(AddSurvey("Val B", TrainingSplit.Validation), "b.jpg");
        AddBox(AddImage(AddSurvey("Test C", TrainingSplit.Test), "c.jpg"));
        AddImage(AddSurvey("Colour", TrainingSplit.Train, CameraType.Rgb), "rgb.jpg");   // R5: other camera not exported

        var result = await _service.ExportAsync(new ExportTrainingDataRequest(CameraType.Thermal), "gaurav");

        Assert.Equal("wfp/thermal-20261019-090000", _writer.Begun);                       // R7
        Assert.True(_writer.Completed);
        Assert.EndsWith(@"wfp\thermal-20261019-090000", result.Folder);
        Assert.Equal(3, _writer.Images.Count);                                             // flagged + RGB left out
        Assert.Equal(pigPhoto.StoragePath, _writer.Images[$"images/train/Train_A_{pigPhoto.Id}.jpg"]);
        Assert.Equal("0 0.125000 0.312500 0.050000 0.125000\n", _writer.Texts[$"labels/train/Train_A_{pigPhoto.Id}.txt"]);
        Assert.Equal(string.Empty, _writer.Texts[$"labels/val/Val_B_{emptyPhoto.Id}.txt"]);   // R4
        Assert.Contains("names:\n  0: pig", _writer.Texts["data.yaml"]);
        Assert.Contains("\"camera\": \"thermal\"", _writer.Texts["dataset-card.json"]);
        Assert.Contains("\"createdBy\": \"gaurav\"", _writer.Texts["dataset-card.json"]);

        var totals = result.Totals.ToDictionary(t => t.Split);
        Assert.Equal((1, 1, 1), (totals[TrainingSplit.Train].Photos, totals[TrainingSplit.Train].PhotosWithLabels, totals[TrainingSplit.Train].Labels));
        Assert.Equal((1, 0, 0), (totals[TrainingSplit.Validation].Photos, totals[TrainingSplit.Validation].PhotosWithLabels, totals[TrainingSplit.Validation].Labels));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Export_is_refused_when_a_set_has_no_usable_photo()
    {
        AddImage(AddSurvey("Train", TrainingSplit.Train), "a.jpg");
        AddImage(AddSurvey("Test", TrainingSplit.Test), "c.jpg");
        AddImage(AddSurvey("Val", TrainingSplit.Validation), "b.jpg", reviewed: false);   // only an unchecked photo

        await Assert.ThrowsAsync<RequestValidationException>(() => _service.ExportAsync(new ExportTrainingDataRequest(CameraType.Thermal), null));
        Assert.Null(_writer.Begun);                                                        // R6: nothing written
    }

    [Fact]
    public async Task Export_skips_photos_without_a_size_or_file_and_says_so()
    {
        var train = AddSurvey("Train", TrainingSplit.Train);
        AddImage(train, "good.jpg");
        AddImage(train, "nosize.jpg", width: null);
        var gone = AddImage(train, "gone.jpg");
        _writer.MissingKeys.Add(gone.StoragePath);
        AddImage(AddSurvey("Val", TrainingSplit.Validation), "b.jpg");
        AddImage(AddSurvey("Test", TrainingSplit.Test), "c.jpg");

        var result = await _service.ExportAsync(new ExportTrainingDataRequest(CameraType.Thermal), null);

        Assert.Equal(1, result.Totals.Single(t => t.Split == TrainingSplit.Train).Photos);   // R8
        Assert.Contains(result.Warnings, w => w.Contains("nosize.jpg") && w.Contains("size unknown"));
        Assert.Contains(result.Warnings, w => w.Contains("gone.jpg") && w.Contains("missing"));
        Assert.DoesNotContain(_writer.Texts.Keys, k => k.Contains(gone.Id.ToString()));
    }

    // ---- helpers ----

    private Survey AddSurvey(string name, TrainingSplit? split, CameraType camera = CameraType.Thermal)
    {
        var s = new Survey
        {
            Id = Guid.NewGuid(), Name = name, SiteName = "Katherine", SurveyDate = new DateOnly(2021, 2, 14),
            StartTime = new TimeOnly(6, 0), Camera = camera, Status = SurveyStatus.Reviewed, TrainingSplit = split,
        };
        _db.Surveys.Add(s);
        return s;
    }

    private SurveyImage AddImage(Survey s, string file, bool reviewed = true, bool flagged = false, int? width = 640)
    {
        var id = Guid.NewGuid();
        var i = new SurveyImage
        {
            Id = id, SurveyId = s.Id, FileName = file, StoragePath = $"wfp/surveys/{s.Id}/originals/{id}.jpg",
            WidthPx = width, HeightPx = 512, Reviewed = reviewed, Flagged = flagged, Camera = s.Camera,
        };
        _db.Images.Add(i);
        return i;
    }

    private void AddBox(SurveyImage image, DetectionStatus status = DetectionStatus.Accepted) =>
        _db.Detections.Add(new AnimalDetection
        {
            Id = Guid.NewGuid(), ImageId = image.Id, X = 64, Y = 128, Width = 32, Height = 64,
            Source = DetectionSource.Ranger, Status = status,
        });
}
