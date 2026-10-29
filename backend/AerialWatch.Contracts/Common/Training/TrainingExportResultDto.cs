namespace AerialWatch.Contracts.Common.Training;

/// <summary>Result of one dataset export: where it was written, what is in it and what was skipped (D-038).</summary>
public sealed record TrainingExportResultDto(
    string DatasetName,
    string Folder,
    IReadOnlyList<TrainingSplitTotalsDto> Totals,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAt);
