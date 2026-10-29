namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>
/// One confirmed pig's position on the twin photo (D-040). Number matches the box label on the counted photo.
/// Box (twin's original pixels) is null when NotVisible, or when no suggestion can be made (photo sizes unknown).
/// </summary>
public sealed record TwinMarkDto(
    Guid DetectionId,
    int Number,
    TwinMarkState State,
    double? X,
    double? Y,
    double? Width,
    double? Height);
