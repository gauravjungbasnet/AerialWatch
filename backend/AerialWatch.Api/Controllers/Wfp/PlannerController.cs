using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.Planner.Models;
using AerialWatch.Contracts.Wfp.Planner.Services;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>Survey planning: chance a pig is visible by time window (Hvala et al. 2023).</summary>
public sealed class PlannerController : AppControllerBase
{
    private readonly IPlannerService _planner;

    public PlannerController(IPlannerService planner) => _planner = planner;

    [HttpGet]
    [ProducesResponseType<PlannerResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlannerResult>> GetWindows(
        [FromQuery] string? camera, [FromQuery] string? season, [FromQuery] string? date, CancellationToken ct) =>
        Ok(await _planner.GetWindowsAsync(camera, season, date, ct));

    [HttpPost]
    [ProducesResponseType<ProbabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProbabilityResponse>> GetProbability([FromBody] ProbabilityRequest request, CancellationToken ct) =>
        Ok(await _planner.GetProbabilityAsync(request, ct));
}
