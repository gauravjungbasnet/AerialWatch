namespace AerialWatch.Contracts.Wfp.ModelParameters.Models;

/// <summary>One saved version (a row in wfp.ModelCoefficientSets with its values), newest first.</summary>
public sealed record ModelParametersHistoryItem(
    Guid Id,
    string Version,
    bool IsPublished,
    DateTimeOffset SavedAt,
    string? SavedBy,
    IReadOnlyList<CoefficientEntry> Values);
