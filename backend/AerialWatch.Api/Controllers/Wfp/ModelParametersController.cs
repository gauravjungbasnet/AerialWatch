using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Wfp.ModelParameters.Models;
using AerialWatch.Contracts.Wfp.ModelParameters.Services;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>
/// Admin: view and edit the detection-model coefficients used by every calculation (stored in SQL Server,
/// table ModelCoefficientSets; every save adds a row).
/// Requires the Admin role. Development bypass supplies a local admin identity only in Development.
/// </summary>
[Authorize(Roles = "Admin")]
public sealed class ModelParametersController : AppControllerBase
{
    private readonly IModelParametersService _parameters;

    public ModelParametersController(IModelParametersService parameters) => _parameters = parameters;

    [HttpGet]
    [ProducesResponseType<ModelParametersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelParametersResponse>> Get(CancellationToken ct) => Ok(await _parameters.GetAsync(ct));

    [HttpPut]
    [ProducesResponseType<ModelParametersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModelParametersResponse>> Update([FromBody] UpdateModelParametersRequest request, CancellationToken ct) =>
        Ok(await _parameters.UpdateAsync(request, CurrentUserName, ct));

    [HttpPost]
    [ProducesResponseType<ModelParametersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelParametersResponse>> Reset(CancellationToken ct) =>
        Ok(await _parameters.ResetToPublishedAsync(CurrentUserName, ct));

    /// <summary>Saved versions, newest first (audit trail).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ModelParametersHistoryItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModelParametersHistoryItem>>> History([FromQuery] int take = 20, CancellationToken ct = default) =>
        Ok(await _parameters.GetHistoryAsync(take, ct));

    private string? CurrentUserName => User.Identity?.Name;
}
