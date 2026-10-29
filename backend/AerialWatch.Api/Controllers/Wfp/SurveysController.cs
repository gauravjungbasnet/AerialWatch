using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Surveys.Models;
using AerialWatch.Contracts.Wfp.Surveys.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>Drone surveys (one flight each). Images, detections and estimates are added in later slices.</summary>
public sealed class SurveysController : AppControllerBase
{
    private readonly ISurveyService _surveys;

    public SurveysController(ISurveyService surveys) => _surveys = surveys;

    /// <summary>GET /api/wfp/surveys/list?search=&amp;date=&amp;camera=&amp;status=&amp;sort=&amp;dir=&amp;page=&amp;pageSize= (server-side paging).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SurveySummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SurveySummaryDto>>> List([FromQuery] SurveyListQuery query, CancellationToken ct) =>
        Ok(await _surveys.ListAsync(query, ct));

    /// <summary>Totals for the dashboard cards.</summary>
    [HttpGet]
    [ProducesResponseType<SurveyStatsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SurveyStatsDto>> Stats(CancellationToken ct) => Ok(await _surveys.GetStatsAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SurveyDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _surveys.GetAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<SurveyDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SurveyDetailDto>> Create([FromBody] CreateSurveyRequest request, CancellationToken ct)
    {
        var created = await _surveys.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _surveys.DeleteAsync(id, ct);
        return NoContent();
    }
}
