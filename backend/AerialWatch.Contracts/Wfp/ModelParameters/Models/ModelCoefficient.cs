using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.ModelParameters.Models;

/// <summary>
/// One coefficient as shown on the admin page: its type (from wfp.CoefficientTypes), the published value and the value
/// in use. The page groups rows by Model and Camera and orders them by SortOrder.
/// </summary>
public sealed record ModelCoefficient(
    string Code,
    CameraType Camera,
    DetectionModelKind Model,
    CoefficientTerm Term,
    string Label,
    string Description,
    int SortOrder,
    double Published,
    double Current);
