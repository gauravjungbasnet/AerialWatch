using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.ModelParameters;

/// <summary>The coefficient set currently in use, plus whether it is the published one.</summary>
public sealed record ModelParametersState(
    DetectionModelParameters Parameters,
    bool IsPublished,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy);
