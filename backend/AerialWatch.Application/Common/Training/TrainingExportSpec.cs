namespace AerialWatch.Application.Common.Training;

/// <summary>
/// What to export (D-038). DatasetName is a relative folder name such as "wfp/thermal-20261027-101500"
/// (lower-case letters, digits and '-', at most one '/'). ClassNames become data.yaml "names" (index = ClassId).
/// CardExtras are added to dataset-card.json (e.g. camera).
/// </summary>
public sealed record TrainingExportSpec(
    string DatasetName,
    IReadOnlyList<string> ClassNames,
    IReadOnlyList<TrainingDatasetItem> Items,
    IReadOnlyDictionary<string, object?> CardExtras,
    string? CreatedBy);
