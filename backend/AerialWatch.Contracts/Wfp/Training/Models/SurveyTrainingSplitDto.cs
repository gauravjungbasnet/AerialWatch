using AerialWatch.Domain.Common.Training;

namespace AerialWatch.Contracts.Wfp.Training.Models;

/// <summary>A survey's training set after a change; null = not used for training.</summary>
public sealed record SurveyTrainingSplitDto(Guid SurveyId, TrainingSplit? Split);
