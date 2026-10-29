using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.Training;

/// <summary>Read-only queries for AI training data (D-038). Implemented in Infrastructure with SQL Server.</summary>
public interface ITrainingDataRepository
{
    /// <summary>Every survey with a training split, with photo/pig counts.</summary>
    Task<IReadOnlyList<TrainingSurveyRow>> ListTaggedSurveysAsync(CancellationToken ct = default);

    /// <summary>Usable photos (checked, not flagged) of tagged surveys of one camera, with accepted pig boxes; by survey then file name.</summary>
    Task<IReadOnlyList<TrainingImageRow>> ListExportImagesAsync(CameraType camera, CancellationToken ct = default);
}
