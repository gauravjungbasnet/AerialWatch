namespace AerialWatch.Contracts.Wfp.ModelParameters.Models;

public sealed record ModelParametersResponse(
    string Version,
    bool IsPublished,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<ModelCoefficient> Coefficients,
    string Source);
