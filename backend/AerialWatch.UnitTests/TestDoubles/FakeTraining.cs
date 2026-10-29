using AerialWatch.Application.Common.Training;
using AerialWatch.Application.Wfp.Training;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.UnitTests.TestDoubles;

/// <summary>Same queries as TrainingDataRepository (D-038), over the FakeDatabase lists.</summary>
internal sealed class FakeTrainingDataRepository : ITrainingDataRepository
{
    private readonly FakeDatabase _db;
    public FakeTrainingDataRepository(FakeDatabase db) => _db = db;

    private static bool IsPig(AnimalDetection d) => d.Status == DetectionStatus.Accepted && d.Label == AnimalDetection.PigLabel;
    private static bool IsUsable(SurveyImage i) => i.Reviewed && !i.Flagged;
    private int PigsOn(SurveyImage i) => _db.Detections.Count(d => d.ImageId == i.Id && IsPig(d));

    public Task<IReadOnlyList<TrainingSurveyRow>> ListTaggedSurveysAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TrainingSurveyRow>>(_db.Surveys.Where(s => s.TrainingSplit != null).Select(s =>
        {
            var images = _db.Images.Where(i => i.SurveyId == s.Id && i.Camera == s.Camera).ToList();
            var usable = images.Where(IsUsable).ToList();
            return new TrainingSurveyRow(s.Id, s.Name, s.SurveyDate, s.StartTime, s.Camera, s.Status, s.TrainingSplit!.Value,
                images.Count, images.Count(i => i.Reviewed), images.Count(i => i.Flagged), usable.Count,
                usable.Count(i => PigsOn(i) > 0), usable.Sum(PigsOn));
        }).ToList());

    public Task<IReadOnlyList<TrainingImageRow>> ListExportImagesAsync(CameraType camera, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TrainingImageRow>>((
            from i in _db.Images
            join s in _db.Surveys on i.SurveyId equals s.Id
            where IsUsable(i) && s.TrainingSplit != null && s.Camera == camera && i.Camera == camera
            orderby s.Name, i.FileName
            select new TrainingImageRow(i.Id, s.Name, s.TrainingSplit!.Value, i.FileName, i.StoragePath, i.WidthPx, i.HeightPx,
                _db.Detections.Where(d => d.ImageId == i.Id && IsPig(d)).Select(d => new PigBox(d.X, d.Y, d.Width, d.Height)).ToList())
        ).ToList());
}

/// <summary>In-memory dataset writer: records copied images (relative path → storage key) and text files.</summary>
internal sealed class FakeDatasetWriter : ITrainingDatasetWriter
{
    public string ExportRoot => @"C:\exports";
    public HashSet<string> MissingKeys { get; } = new();
    public Dictionary<string, string> Images { get; } = new();
    public Dictionary<string, string> Texts { get; } = new();
    public string? Begun { get; private set; }
    public bool Completed { get; private set; }

    public Task<string> BeginAsync(string datasetName, CancellationToken ct = default)
    {
        Begun = datasetName;
        return Task.FromResult(datasetName + ".partial");
    }

    public Task<bool> CopyImageAsync(string handle, string storageKey, string relativePath, CancellationToken ct = default)
    {
        if (MissingKeys.Contains(storageKey)) return Task.FromResult(false);
        Images[relativePath] = storageKey;
        return Task.FromResult(true);
    }

    public Task WriteTextAsync(string handle, string relativePath, string text, CancellationToken ct = default)
    {
        Texts[relativePath] = text;
        return Task.CompletedTask;
    }

    public Task<string> CompleteAsync(string handle, CancellationToken ct = default)
    {
        Completed = true;
        return Task.FromResult($@"{ExportRoot}\{Begun!.Replace('/', '\\')}");
    }
}
