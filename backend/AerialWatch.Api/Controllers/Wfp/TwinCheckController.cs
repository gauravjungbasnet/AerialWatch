using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.TwinCheck.Models;
using AerialWatch.Contracts.Wfp.TwinCheck.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>
/// Thermal/colour twin check (D-040) under /api/wfp/twinCheck: suggested and checked positions of confirmed pigs on the
/// other camera's photo, set or clear one, and the survey summary. The signed-in user's name is recorded.
/// </summary>
public sealed class TwinCheckController : AppControllerBase
{
    private readonly ITwinCheckService _twins;

    public TwinCheckController(ITwinCheckService twins) => _twins = twins;

    /// <summary>GET get/{imageId}: marks for the confirmed pigs on a counted photo.</summary>
    [HttpGet("{imageId:guid}")]
    [ProducesResponseType<TwinCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TwinCheckDto>> Get(Guid imageId, CancellationToken ct) =>
        Ok(await _twins.GetAsync(imageId, ct));

    /// <summary>PUT set/{detectionId} { state, x, y, width, height }: confirm/move, or "not visible".</summary>
    [HttpPut("{detectionId:guid}")]
    [ProducesResponseType<TwinMarkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TwinMarkDto>> Set(Guid detectionId, [FromBody] SetTwinMarkRequest request, CancellationToken ct) =>
        Ok(await _twins.SetAsync(detectionId, request, User.Identity?.Name, ct));

    /// <summary>DELETE clear/{detectionId}: back to the suggestion.</summary>
    [HttpDelete("{detectionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Clear(Guid detectionId, CancellationToken ct)
    {
        await _twins.ClearAsync(detectionId, ct);
        return NoContent();
    }

    /// <summary>POST addSighting/{imageId} { x, y, width, height }: a pig drawn first on the twin of this counted photo (D-041).</summary>
    [HttpPost("{imageId:guid}")]
    [ProducesResponseType<TwinSightingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TwinSightingDto>> AddSighting(Guid imageId, [FromBody] TwinBoxRequest request, CancellationToken ct) =>
        Ok(await _twins.AddSightingAsync(imageId, request, User.Identity?.Name, ct));

    /// <summary>POST confirmSighting/{markId} { x, y, width, height }: the sighting is here on the counted photo; creates the counted pig.</summary>
    [HttpPost("{markId:guid}")]
    [ProducesResponseType<TwinMarkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TwinMarkDto>> ConfirmSighting(Guid markId, [FromBody] TwinBoxRequest request, CancellationToken ct) =>
        Ok(await _twins.ConfirmSightingAsync(markId, request, User.Identity?.Name, ct));

    /// <summary>PUT sightingVisibility/{markId} { notVisibleOnCounted }: can't be seen on the counted photo (or undo).</summary>
    [HttpPut("{markId:guid}")]
    [ProducesResponseType<TwinSightingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TwinSightingDto>> SightingVisibility(Guid markId, [FromBody] SetSightingVisibilityRequest request, CancellationToken ct) =>
        Ok(await _twins.SetSightingVisibilityAsync(markId, request, User.Identity?.Name, ct));

    /// <summary>DELETE removeSighting/{markId}</summary>
    [HttpDelete("{markId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSighting(Guid markId, CancellationToken ct)
    {
        await _twins.RemoveSightingAsync(markId, ct);
        return NoContent();
    }

    /// <summary>GET summary/{surveyId}: how many confirmed pigs were seen on the other camera.</summary>
    [HttpGet("{surveyId:guid}")]
    [ProducesResponseType<TwinCheckSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TwinCheckSummaryDto>> Summary(Guid surveyId, CancellationToken ct) =>
        Ok(await _twins.GetSummaryAsync(surveyId, ct));
}
