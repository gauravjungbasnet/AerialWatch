using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.Common;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Contracts.Wfp.Estimates.Models;
using AerialWatch.Contracts.Wfp.Estimates.Services;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Application.Wfp.Estimates;

public sealed class EstimateService : IEstimateService
{
    private readonly IActiveModelParameters _parameters;

    public EstimateService(IActiveModelParameters parameters) => _parameters = parameters;

    public async Task<EstimateResponse> PreviewAsync(EstimatePreviewRequest request, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        var notes = new List<Assumption>();

        if (request.ConfirmedCount < 0) errors.Add("confirmedCount", "Count cannot be negative.");
        if (request.AreaKm2 is <= 0) errors.Add("areaKm2", "Area must be greater than 0.");

        var season = ConditionsResolver.ResolveSeason(request.Season, request.SurveyDate, errors, notes, "surveyDate");
        var window = ConditionsResolver.ResolveWindow(null, request.StartTime, errors, notes, "startTime");
        errors.ThrowIfAny();

        var state = await _parameters.GetAsync(ct);
        var detection = ConditionsResolver.Compute(request.Model, request.Camera, window, season,
            request.AmbientTemperatureC, request.CanopyProportion, errors, state.Parameters);
        errors.ThrowIfAny();

        var estimate = PopulationEstimator.Estimate(request.ConfirmedCount, detection!.Probability, request.AreaKm2);

        var assumptions = ModelParameterNotes.For(state)
            .Concat(notes)
            .Concat(detection.Assumptions)
            .Concat(Hvala2023Coefficients.StudyConditions.Select(Assumption.Info))
            .Append(Assumption.Warning("The model was fitted on feral pigs at one NT site. Other species or very different habitats are estimates only."))
            .ToList();

        return new EstimateResponse(
            estimate.ConfirmedCount,
            Math.Round(estimate.DetectionProbability, 4),
            Math.Round(estimate.Estimate, 2),
            estimate.EstimateRounded,
            estimate.Low,
            estimate.High,
            estimate.DensityPerKm2,
            estimate.Method,
            detection.Model,
            detection.ModelVersion,
            window,
            season!.Value,
            Provisional: false,
            assumptions);
    }
}
