namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>
/// A pig drawn first on the twin photo (D-041). Label is "c1", "c2"… on a colour twin ("t1"… on a thermal twin).
/// TwinX/Y/Width/Height = where it was drawn on the twin; X/Y/Width/Height = suggested position on the counted photo
/// (null when photo sizes are unknown). Confirming it there turns it into a counted pig.
/// </summary>
public sealed record TwinSightingDto(
    Guid MarkId,
    string Label,
    TwinSightingState State,
    double TwinX,
    double TwinY,
    double TwinWidth,
    double TwinHeight,
    double? X,
    double? Y,
    double? Width,
    double? Height);
