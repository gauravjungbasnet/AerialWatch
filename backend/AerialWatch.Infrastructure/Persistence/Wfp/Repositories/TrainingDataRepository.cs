using Microsoft.EntityFrameworkCore;
using AerialWatch.Application.Wfp.Training;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Infrastructure.Persistence.Wfp.Repositories;

/// <summary>
/// AI training data queries (D-038). All counts run in SQL Server. Usable photo = checked (Reviewed) and not flagged;
/// only photos from the survey's own camera (D-039); a pig = an Accepted box labelled "pig" (AI-accepted or ranger-drawn). Read-only (no tracking).
/// </summary>
public sealed class TrainingDataRepository : ITrainingDataRepository
{
    private readonly AerialWatchDbContext _db;

    public TrainingDataRepository(AerialWatchDbContext db) => _db = db;

    public async Task<IReadOnlyList<TrainingSurveyRow>> ListTaggedSurveysAsync(CancellationToken ct = default) =>
        await _db.Surveys.AsNoTracking()
            .Where(s => s.TrainingSplit != null)
            .Select(s => new TrainingSurveyRow(
                s.Id, s.Name, s.SurveyDate, s.StartTime, s.Camera, s.Status, s.TrainingSplit!.Value,
                s.Images.Count(i => i.Camera == s.Camera),
                s.Images.Count(i => i.Camera == s.Camera && i.Reviewed),
                s.Images.Count(i => i.Camera == s.Camera && i.Flagged),
                s.Images.Count(i => i.Camera == s.Camera && i.Reviewed && !i.Flagged),
                s.Images.Count(i => i.Camera == s.Camera && i.Reviewed && !i.Flagged
                    && i.Detections.Any(d => d.Status == DetectionStatus.Accepted && d.Label == AnimalDetection.PigLabel)),
                s.Images.Where(i => i.Camera == s.Camera && i.Reviewed && !i.Flagged)
                    .SelectMany(i => i.Detections)
                    .Count(d => d.Status == DetectionStatus.Accepted && d.Label == AnimalDetection.PigLabel)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TrainingImageRow>> ListExportImagesAsync(CameraType camera, CancellationToken ct = default)
    {
        var rows = await _db.SurveyImages.AsNoTracking()
            .Where(i => i.Reviewed && !i.Flagged && i.Survey!.TrainingSplit != null && i.Survey.Camera == camera
                && i.Camera == camera) // reference twins from the other camera are never exported (D-039)
            .OrderBy(i => i.Survey!.Name).ThenBy(i => i.FileName)
            .Select(i => new
            {
                i.Id,
                SurveyName = i.Survey!.Name,
                Split = i.Survey.TrainingSplit!.Value,
                i.FileName,
                i.StoragePath,
                i.WidthPx,
                i.HeightPx,
                Boxes = i.Detections
                    .Where(d => d.Status == DetectionStatus.Accepted && d.Label == AnimalDetection.PigLabel)
                    .Select(d => new PigBox(d.X, d.Y, d.Width, d.Height))
                    .ToList(),
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new TrainingImageRow(r.Id, r.SurveyName, r.Split, r.FileName, r.StoragePath, r.WidthPx, r.HeightPx, r.Boxes))
            .ToList();
    }
}
