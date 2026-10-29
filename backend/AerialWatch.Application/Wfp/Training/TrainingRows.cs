using AerialWatch.Domain.Common.Training;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Training;

/// <summary>A tagged survey with counts computed in SQL (D-038). Usable = checked and not flagged; Pigs = accepted pig boxes on usable photos.</summary>
public sealed record TrainingSurveyRow(
    Guid Id, string Name, DateOnly SurveyDate, TimeOnly StartTime, CameraType Camera, SurveyStatus Status, TrainingSplit Split,
    int Photos, int CheckedPhotos, int FlaggedPhotos, int UsablePhotos, int PhotosWithPigs, int Pigs);

/// <summary>An accepted pig box in original pixels.</summary>
public sealed record PigBox(double X, double Y, double Width, double Height);

/// <summary>A usable photo of a tagged survey, with its accepted pig boxes.</summary>
public sealed record TrainingImageRow(
    Guid ImageId, string SurveyName, TrainingSplit Split, string FileName, string StoragePath,
    int? WidthPx, int? HeightPx, IReadOnlyList<PigBox> Boxes);
