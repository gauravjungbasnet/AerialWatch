using AerialWatch.Domain.Common.Training;

namespace AerialWatch.Contracts.Common.Training;

/// <summary>
/// Totals for one set of a training dataset (D-038). Shared by all modules.
/// Photos = usable photos (checked, not flagged); Labels = boxes that will be written.
/// </summary>
public sealed record TrainingSplitTotalsDto(TrainingSplit Split, int Sessions, int Photos, int PhotosWithLabels, int Labels);
