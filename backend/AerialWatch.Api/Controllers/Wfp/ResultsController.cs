using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.Results.Models;
using AerialWatch.Contracts.Wfp.Results.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>Survey results (Slice 5) under /api/wfp/results: confirmed count, corrected estimate, photo locations.</summary>
public sealed class ResultsController : AppControllerBase
{
    private readonly ISurveyResultsService _results;

    public ResultsController(ISurveyResultsService results) => _results = results;

    /// <summary>GET get/{surveyId}</summary>
    [HttpGet("{surveyId:guid}")]
    [ProducesResponseType<SurveyResultsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyResultsDto>> Get(Guid surveyId, CancellationToken ct) =>
        Ok(await _results.GetAsync(surveyId, ct));
}
