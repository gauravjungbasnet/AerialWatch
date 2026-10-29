using AerialWatch.Contracts.Common.Training;
using AerialWatch.Contracts.Wfp.Training.Models;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Training.Services;

/// <summary>
/// AI training data for Watch Feral Pigs (D-038): tag surveys as train/validation/test, see the totals,
/// export a YOLO dataset to a local folder. The dataset rules live in the shared exporter.
/// </summary>
public interface ITrainingDataService
{
    /// <summary>Tagged surveys of one camera with counts, totals per set and warnings.</summary>
    Task<TrainingOverviewDto> GetOverviewAsync(CameraType camera, CancellationToken ct = default);

    /// <summary>Puts a survey in a set, or removes it (null). 404 if the survey doesn't exist.</summary>
    Task<SurveyTrainingSplitDto> SetSplitAsync(Guid surveyId, SetTrainingSplitRequest request, CancellationToken ct = default);

    /// <summary>Writes the dataset folder. 400 if train, validation or test has no usable photo.</summary>
    Task<TrainingExportResultDto> ExportAsync(ExportTrainingDataRequest request, string? user, CancellationToken ct = default);
}
