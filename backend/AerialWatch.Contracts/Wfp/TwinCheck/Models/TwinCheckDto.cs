namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>
/// Twin check for one counted photo (D-040, D-041). TwinImageId is null when the photo has no twin.
/// Marks = confirmed pigs on the counted photo and where they are on the twin; Sightings = pigs drawn first on the twin.
/// LearnedFrom = how many ranger-placed pairs in this survey the suggestions are fitted to (0 = simple size scaling).
/// </summary>
public sealed record TwinCheckDto(
    Guid ImageId,
    Guid? TwinImageId,
    int LearnedFrom,
    IReadOnlyList<TwinMarkDto> Marks,
    IReadOnlyList<TwinSightingDto> Sightings);
