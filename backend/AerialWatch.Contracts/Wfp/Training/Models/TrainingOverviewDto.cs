using AerialWatch.Contracts.Common.Training;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.Training.Models;

/// <summary>Admin › AI training data for one camera: tagged surveys, totals per set, warnings and whether export is possible.</summary>
public sealed record TrainingOverviewDto(
    CameraType Camera,
    string ExportRoot,
    IReadOnlyList<TrainingSurveyDto> Surveys,
    IReadOnlyList<TrainingSplitTotalsDto> Totals,
    IReadOnlyList<string> Warnings,
    bool CanExport);
