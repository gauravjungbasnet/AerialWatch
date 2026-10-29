using AerialWatch.Domain.Common.Training;

namespace AerialWatch.Application.Common.Training;

/// <summary>
/// One photo going into a training dataset, in module-neutral form (D-038). A module (e.g. WFP) builds these from its own
/// tables; the shared exporter turns them into a YOLO dataset. SessionName is the capture session the split belongs to
/// (a survey/flight); StorageKey is the photo's IFileStorage key.
/// </summary>
public sealed record TrainingDatasetItem(
    string SessionName,
    TrainingSplit Split,
    string StorageKey,
    string FileName,
    int? WidthPx,
    int? HeightPx,
    IReadOnlyList<TrainingLabel> Labels);
