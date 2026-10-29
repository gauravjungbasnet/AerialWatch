namespace AerialWatch.Contracts.Wfp.ModelInfo.Models;

public sealed record ModelInfoResponse(
    string ModelVersion,
    string Citation,
    string Doi,
    IReadOnlyList<string> StudyConditions,
    object Coefficients,
    IReadOnlyList<string> OurAssumptions);
