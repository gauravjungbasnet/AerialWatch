using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>Review progress for a survey. ConfirmedPigs is the count C used by the estimate (Slice 4).</summary>
public sealed record ReviewSummaryDto(
    Guid SurveyId,
    SurveyStatus Status,
    int Photos,
    int ReviewedPhotos,
    int FlaggedPhotos,
    int PhotosWithPigs,
    int ConfirmedPigs,
    int PendingSuggestions,
    int RejectedSuggestions,
    int AiSuggestions,
    int RangerAdded,
    // True when every photo is checked and no suggestion is pending.
    bool CanComplete,
    // Why the review can't be completed yet (null when it can).
    string? BlockedReason);
