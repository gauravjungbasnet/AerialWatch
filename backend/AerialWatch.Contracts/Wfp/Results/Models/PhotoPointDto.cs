namespace AerialWatch.Contracts.Wfp.Results.Models;

/// <summary>One photo for the results map/table: where and when it was taken and how many pigs were confirmed on it.</summary>
public sealed record PhotoPointDto(
    Guid ImageId,
    string FileName,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? TakenAt,
    int Confirmed,
    bool Reviewed,
    bool Flagged);
