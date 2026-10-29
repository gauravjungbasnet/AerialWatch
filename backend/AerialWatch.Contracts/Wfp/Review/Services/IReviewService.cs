using AerialWatch.Contracts.Wfp.Review.Models;

namespace AerialWatch.Contracts.Wfp.Review.Services;

/// <summary>
/// Ranger review of a survey's photos (D-034): check AI suggestions, draw boxes the AI missed, mark photos as checked.
/// Changes are allowed only while the survey is InReview. <paramref name="user"/> is the signed-in name (audit).
/// </summary>
public interface IReviewService
{
    /// <summary>Moves a survey with photos from Uploading/Processing to InReview (photos are then locked). Repeat calls are harmless.</summary>
    Task<ReviewSummaryDto> StartAsync(Guid surveyId, CancellationToken ct = default);

    Task<ReviewSummaryDto> GetSummaryAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>The review queue, in flight order.</summary>
    Task<IReadOnlyList<ReviewImageDto>> ListImagesAsync(Guid surveyId, ReviewImageFilter filter, CancellationToken ct = default);

    Task<ReviewImageDetailDto> GetImageAsync(Guid imageId, CancellationToken ct = default);

    /// <summary>Adds a ranger box (counted as a confirmed pig).</summary>
    Task<DetectionDto> AddDetectionAsync(Guid imageId, AddDetectionRequest request, string? user, CancellationToken ct = default);

    Task<DetectionDto> UpdateDetectionAsync(Guid detectionId, UpdateDetectionRequest request, string? user, CancellationToken ct = default);

    /// <summary>Deletes a ranger box. AI suggestions are rejected instead, so the AI's accuracy can still be measured.</summary>
    Task DeleteDetectionAsync(Guid detectionId, CancellationToken ct = default);

    /// <summary>Marks a photo as checked. Fails while it still has pending suggestions.</summary>
    Task<ReviewImageDto> CompleteImageAsync(Guid imageId, string? user, CancellationToken ct = default);

    Task<ReviewImageDto> ReopenImageAsync(Guid imageId, CancellationToken ct = default);

    Task<ReviewImageDto> FlagImageAsync(Guid imageId, FlagImageRequest request, CancellationToken ct = default);

    Task<BulkAcceptResponse> BulkAcceptAsync(Guid surveyId, BulkAcceptRequest request, string? user, CancellationToken ct = default);

    /// <summary>InReview → Reviewed, once every photo is checked and nothing is pending.</summary>
    Task<ReviewSummaryDto> CompleteSurveyAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>Reviewed → InReview, to correct something.</summary>
    Task<ReviewSummaryDto> ReopenSurveyAsync(Guid surveyId, CancellationToken ct = default);
}
