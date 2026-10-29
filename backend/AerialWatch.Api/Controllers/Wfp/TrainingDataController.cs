using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Controllers.Shared;
using AerialWatch.Contracts.Common.Training;
using AerialWatch.Contracts.Wfp.Training.Models;
using AerialWatch.Contracts.Wfp.Training.Services;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Api.Controllers.Wfp;

/// <summary>
/// Admin › AI training data (D-038), under /api/wfp/trainingData: tag surveys as train/validation/test, see totals and
/// warnings per camera, export a YOLO dataset to the local export folder. Admin only, like Model parameters.
/// </summary>
[Authorize(Roles = "Admin")]
public sealed class TrainingDataController : AppControllerBase
{
    private readonly ITrainingDataService _training;

    public TrainingDataController(ITrainingDataService training) => _training = training;

    /// <summary>GET overview?camera=thermal|rgb: tagged surveys, totals per set, warnings, export folder.</summary>
    [HttpGet]
    [ProducesResponseType<TrainingOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrainingOverviewDto>> Overview([FromQuery] CameraType camera = CameraType.Thermal, CancellationToken ct = default) =>
        Ok(await _training.GetOverviewAsync(camera, ct));

    /// <summary>PUT setSplit/{surveyId} { "split": "train" | "validation" | "test" | null }.</summary>
    [HttpPut("{surveyId:guid}")]
    [ProducesResponseType<SurveyTrainingSplitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyTrainingSplitDto>> SetSplit(Guid surveyId, [FromBody] SetTrainingSplitRequest request, CancellationToken ct) =>
        Ok(await _training.SetSplitAsync(surveyId, request, ct));

    /// <summary>POST export { "camera": "thermal" }: writes &lt;ExportRoot&gt;\wfp\&lt;camera&gt;-yyyyMMdd-HHmmss.</summary>
    [HttpPost]
    [ProducesResponseType<TrainingExportResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrainingExportResultDto>> Export([FromBody] ExportTrainingDataRequest request, CancellationToken ct) =>
        Ok(await _training.ExportAsync(request, User.Identity?.Name, ct));
}
