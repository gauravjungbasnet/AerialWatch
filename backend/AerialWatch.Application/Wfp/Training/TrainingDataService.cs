using System.Globalization;
using AerialWatch.Application.Common;
using AerialWatch.Application.Common.Training;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Common.Training;
using AerialWatch.Contracts.Wfp.Training.Models;
using AerialWatch.Contracts.Wfp.Training.Services;
using AerialWatch.Domain.Common.Training;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Training;

/// <summary>
/// WFP adapter for AI training data (D-038): tags surveys, reports counts, and hands usable photos with accepted pig
/// boxes (rules R1–R3, R5) to the shared <see cref="TrainingDatasetExporter"/>.
/// </summary>
public sealed class TrainingDataService : ITrainingDataService
{
    public static readonly IReadOnlyList<string> ClassNames = new[] { AnimalDetection.PigLabel };
    private static readonly TrainingSplit[] Splits = { TrainingSplit.Train, TrainingSplit.Validation, TrainingSplit.Test };

    private readonly ISurveyRepository _surveys;
    private readonly ITrainingDataRepository _data;
    private readonly TrainingDatasetExporter _exporter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public TrainingDataService(ISurveyRepository surveys, ITrainingDataRepository data, TrainingDatasetExporter exporter,
        IUnitOfWork unitOfWork, IClock clock)
    {
        _surveys = surveys;
        _data = data;
        _exporter = exporter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<TrainingOverviewDto> GetOverviewAsync(CameraType camera, CancellationToken ct = default)
    {
        var rows = (await _data.ListTaggedSurveysAsync(ct))
            .Where(r => r.Camera == camera)
            .OrderBy(r => r.SurveyDate).ThenBy(r => r.StartTime).ThenBy(r => r.Name)
            .ToList();

        var totals = Splits.Select(s =>
        {
            var part = rows.Where(r => r.Split == s).ToList();
            return new TrainingSplitTotalsDto(s, part.Count, part.Sum(r => r.UsablePhotos), part.Sum(r => r.PhotosWithPigs), part.Sum(r => r.Pigs));
        }).ToList();

        var warnings = new List<string>();
        foreach (var t in totals.Where(t => t.Photos == 0))
            warnings.Add($"No checked photos in the {t.Split} set yet. Tag a survey as {t.Split} on its survey page.");
        foreach (var r in rows)
        {
            var uncheckedPhotos = r.Photos - r.CheckedPhotos;
            if (uncheckedPhotos > 0) warnings.Add($"{uncheckedPhotos} photo{(uncheckedPhotos == 1 ? "" : "s")} in '{r.Name}' not checked yet, so not exported.");
            if (r.FlaggedPhotos > 0) warnings.Add($"{r.FlaggedPhotos} flagged photo{(r.FlaggedPhotos == 1 ? "" : "s")} in '{r.Name}' left out.");
            if (r.Status != SurveyStatus.Reviewed && r.CheckedPhotos > 0) warnings.Add($"Review of '{r.Name}' isn't complete yet.");
        }

        return new TrainingOverviewDto(camera, _exporter.ExportRoot, rows.Select(ToDto).ToList(), totals, warnings,
            CanExport: totals.All(t => t.Photos > 0));
    }

    public async Task<SurveyTrainingSplitDto> SetSplitAsync(Guid surveyId, SetTrainingSplitRequest request, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct) ?? throw new NotFoundException("Survey", surveyId);
        if (request.Split is { } split && !Enum.IsDefined(split))
            throw new RequestValidationException("split", "Split must be train, validation or test (or empty).");

        survey.TrainingSplit = request.Split;
        survey.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return new SurveyTrainingSplitDto(survey.Id, survey.TrainingSplit);
    }

    public async Task<TrainingExportResultDto> ExportAsync(ExportTrainingDataRequest request, string? user, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.Camera)) throw new RequestValidationException("camera", "Camera must be thermal or rgb.");

        var items = (await _data.ListExportImagesAsync(request.Camera, ct))
            .Select(i => new TrainingDatasetItem(i.SurveyName, i.Split, i.StoragePath, i.FileName, i.WidthPx, i.HeightPx,
                i.Boxes.Select(b => new TrainingLabel(0, b.X, b.Y, b.Width, b.Height)).ToList()))
            .ToList();

        var camera = request.Camera.ToString().ToLowerInvariant();
        var name = $"wfp/{camera}-{_clock.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";
        var extras = new Dictionary<string, object?> { ["module"] = "wfp", ["camera"] = camera };

        return await _exporter.ExportAsync(new TrainingExportSpec(name, ClassNames, items, extras, user), ct);
    }

    private static TrainingSurveyDto ToDto(TrainingSurveyRow r) => new(
        r.Id, r.Name,
        r.SurveyDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        r.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        r.Status, r.Split, r.Photos, r.CheckedPhotos, r.FlaggedPhotos, r.UsablePhotos, r.PhotosWithPigs, r.Pigs);
}
