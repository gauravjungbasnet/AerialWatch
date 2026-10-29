using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.Common;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Contracts.Wfp.Planner.Models;
using AerialWatch.Contracts.Wfp.Planner.Services;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.Planner;

public sealed class PlannerService : IPlannerService
{
    private readonly IActiveModelParameters _parameters;

    public PlannerService(IActiveModelParameters parameters) => _parameters = parameters;

    public async Task<PlannerResult> GetWindowsAsync(string? camera, string? season, string? date, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        var notes = new List<Assumption>();

        if (!ConditionsResolver.TryParseEnum<CameraType>(camera, out var cameraType))
            errors.Add("camera", "camera is required (thermal or rgb).");

        Season? seasonValue = null;
        if (!string.IsNullOrWhiteSpace(season))
        {
            if (ConditionsResolver.TryParseEnum<Season>(season, out var s)) seasonValue = s;
            else errors.Add("season", "season must be wet or dry.");
        }

        var resolved = errors.Has("season") ? null : ConditionsResolver.ResolveSeason(seasonValue, date, errors, notes);
        errors.ThrowIfAny();

        var state = await _parameters.GetAsync(ct);
        var plan = SurveyPlanner.Plan(resolved!.Value, cameraType, state.Parameters);
        return plan with { Assumptions = ModelParameterNotes.For(state).Concat(notes).Concat(plan.Assumptions).ToList() };
    }

    public async Task<ProbabilityResponse> GetProbabilityAsync(ProbabilityRequest request, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        var notes = new List<Assumption>();

        TimeWindow? window = null;
        Season? season = null;
        if (request.Model == DetectionModelKind.TimeSeason)
        {
            window = ConditionsResolver.ResolveWindow(request.Window, request.Time, errors, notes);
            season = ConditionsResolver.ResolveSeason(request.Season, request.Date, errors, notes);
        }

        var state = await _parameters.GetAsync(ct);
        var result = ConditionsResolver.Compute(request.Model, request.Camera, window, season,
            request.AmbientTemperatureC, request.CanopyProportion, errors, state.Parameters);
        errors.ThrowIfAny();

        return new ProbabilityResponse(
            Math.Round(result!.Probability, 4), result.Model, result.ModelVersion, window, season,
            ModelParameterNotes.For(state).Concat(notes).Concat(result.Assumptions).ToList());
    }
}
