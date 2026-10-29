using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.Review.Models;
using AerialWatch.Contracts.Wfp.Review.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>
/// Ranger review tool (D-034), under /api/wfp/review: start/summary/complete/reopen a survey's review, list the photo
/// queue, read one photo with its boxes, add/update/delete boxes, mark photos checked or flagged, bulk-accept.
/// The signed-in user's name is recorded on every change.
/// </summary>
public sealed class ReviewController : AppControllerBase
{
    private readonly IReviewService _review;

    public ReviewController(IReviewService review) => _review = review;

    private string? CurrentUserName => User.Identity?.Name;

    // ---- survey ----

    /// <summary>POST start/{surveyId}: locks the photos and opens the review.</summary>
    [HttpPost("{surveyId:guid}")]
    [ProducesResponseType<ReviewSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewSummaryDto>> Start(Guid surveyId, CancellationToken ct) =>
        Ok(await _review.StartAsync(surveyId, ct));

    [HttpGet("{surveyId:guid}")]
    [ProducesResponseType<ReviewSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewSummaryDto>> Summary(Guid surveyId, CancellationToken ct) =>
        Ok(await _review.GetSummaryAsync(surveyId, ct));

    [HttpPost("{surveyId:guid}")]
    [ProducesResponseType<ReviewSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewSummaryDto>> Complete(Guid surveyId, CancellationToken ct) =>
        Ok(await _review.CompleteSurveyAsync(surveyId, ct));

    [HttpPost("{surveyId:guid}")]
    [ProducesResponseType<ReviewSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewSummaryDto>> Reopen(Guid surveyId, CancellationToken ct) =>
        Ok(await _review.ReopenSurveyAsync(surveyId, ct));

    /// <summary>POST bulkAccept/{surveyId} { minConfidence }: accepts confident AI suggestions in one go.</summary>
    [HttpPost("{surveyId:guid}")]
    [ProducesResponseType<BulkAcceptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkAcceptResponse>> BulkAccept(Guid surveyId, [FromBody] BulkAcceptRequest request, CancellationToken ct) =>
        Ok(await _review.BulkAcceptAsync(surveyId, request, CurrentUserName, ct));

    // ---- photos ----

    /// <summary>GET images/{surveyId}?filter=all|notReviewed|pending|withPigs|noPigs|flagged</summary>
    [HttpGet("{surveyId:guid}")]
    [ProducesResponseType<IReadOnlyList<ReviewImageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReviewImageDto>>> Images(Guid surveyId, [FromQuery] ReviewImageFilter filter, CancellationToken ct) =>
        Ok(await _review.ListImagesAsync(surveyId, filter, ct));

    [HttpGet("{imageId:guid}")]
    [ProducesResponseType<ReviewImageDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewImageDetailDto>> Image(Guid imageId, CancellationToken ct) =>
        Ok(await _review.GetImageAsync(imageId, ct));

    [HttpPost("{imageId:guid}")]
    [ProducesResponseType<ReviewImageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewImageDto>> CompleteImage(Guid imageId, CancellationToken ct) =>
        Ok(await _review.CompleteImageAsync(imageId, CurrentUserName, ct));

    [HttpPost("{imageId:guid}")]
    [ProducesResponseType<ReviewImageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewImageDto>> ReopenImage(Guid imageId, CancellationToken ct) =>
        Ok(await _review.ReopenImageAsync(imageId, ct));

    [HttpPut("{imageId:guid}")]
    [ProducesResponseType<ReviewImageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewImageDto>> FlagImage(Guid imageId, [FromBody] FlagImageRequest request, CancellationToken ct) =>
        Ok(await _review.FlagImageAsync(imageId, request, ct));

    // ---- boxes ----

    /// <summary>POST addDetection/{imageId} { x, y, width, height } in original-image pixels.</summary>
    [HttpPost("{imageId:guid}")]
    [ProducesResponseType<DetectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetectionDto>> AddDetection(Guid imageId, [FromBody] AddDetectionRequest request, CancellationToken ct) =>
        Ok(await _review.AddDetectionAsync(imageId, request, CurrentUserName, ct));

    /// <summary>PUT updateDetection/{id} { x?, y?, width?, height?, status? }</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<DetectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetectionDto>> UpdateDetection(Guid id, [FromBody] UpdateDetectionRequest request, CancellationToken ct) =>
        Ok(await _review.UpdateDetectionAsync(id, request, CurrentUserName, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDetection(Guid id, CancellationToken ct)
    {
        await _review.DeleteDetectionAsync(id, ct);
        return NoContent();
    }
}
