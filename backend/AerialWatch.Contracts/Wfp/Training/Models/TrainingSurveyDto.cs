using AerialWatch.Domain.Common.Training;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Training.Models;

/// <summary>
/// A survey tagged for AI training, with its photo and pig counts (D-038).
/// UsablePhotos = checked and not flagged (the only photos exported); Pigs = accepted pig boxes on usable photos.
/// </summary>
public sealed record TrainingSurveyDto(
    Guid Id,
    string Name,
    string SurveyDate,
    string StartTime,
    SurveyStatus Status,
    TrainingSplit Split,
    int Photos,
    int CheckedPhotos,
    int FlaggedPhotos,
    int UsablePhotos,
    int PhotosWithPigs,
    int Pigs);
