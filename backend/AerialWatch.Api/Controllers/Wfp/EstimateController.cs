using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.Estimates.Models;
using AerialWatch.Contracts.Wfp.Estimates.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>Corrected population estimates (count ÷ chance of being visible).</summary>
public sealed class EstimateController : AppControllerBase
{
    private readonly IEstimateService _estimates;

    public EstimateController(IEstimateService estimates) => _estimates = estimates;

    [HttpPost]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EstimateResponse>> Preview([FromBody] EstimatePreviewRequest request, CancellationToken ct) =>
        Ok(await _estimates.PreviewAsync(request, ct));
}
