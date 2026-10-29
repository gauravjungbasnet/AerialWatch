using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.ModelInfo.Models;
using AerialWatch.Contracts.Wfp.ModelInfo.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>Research model metadata: citation, coefficients, assumptions.</summary>
public sealed class ModelController : AppControllerBase
{
    private readonly IModelInfoService _modelInfo;

    public ModelController(IModelInfoService modelInfo) => _modelInfo = modelInfo;

    [HttpGet]
    [ProducesResponseType<ModelInfoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelInfoResponse>> Info(CancellationToken ct) => Ok(await _modelInfo.GetAsync(ct));
}
