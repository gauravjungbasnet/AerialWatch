namespace AerialWatch.Contracts.Wfp.Results.Models;

/// <summary>Box and photo counts for one survey. Confirmed = accepted boxes (AI-suggested or ranger-drawn).</summary>
public sealed record ResultCountsDto(
    int Photos,
    int ReviewedPhotos,
    int FlaggedPhotos,
    int PhotosWithPigs,
    int PhotosWithGps,
    int Confirmed,
    int ConfirmedFromAi,
    int DrawnByRanger,
    int RejectedAi,
    int PendingAi);
