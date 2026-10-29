using AerialWatch.Domain.Common.Training;

namespace AerialWatch.Contracts.Common.Training;

/// <summary>Put a capture session (e.g. a survey) into a training set; null removes it from training (D-038).</summary>
public sealed record SetTrainingSplitRequest(TrainingSplit? Split);
