using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.Common;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Estimates.Models;
using AerialWatch.Contracts.Wfp.Results.Models;
using AerialWatch.Contracts.Wfp.Results.Services;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Results;

/// <summary>
/// Results for one survey (Slice 5): confirmed pigs C → chance visible p from the survey's camera, season and time
/// (Hvala et al. 2023, time × season model) → estimate N̂ = C ÷ p with a 95% range and density.
/// Only accepted boxes count. The result is provisional until the review is complete.
/// </summary>
public sealed class SurveyResultsService : ISurveyResultsService
{
    private readonly ISurveyRepository _surveys;
    private readonly ISurveyImageRepository _images;
    private readonly IDetectionRepository _detections;
    private readonly IActiveModelParameters _parameters;

    public SurveyResultsService(ISurveyRepository surveys, ISurveyImageRepository images, IDetectionRepository detections,
        IActiveModelParameters parameters)
    {
        _surveys = surveys;
        _images = images;
        _detections = detections;
        _parameters = parameters;
    }

    public async Task<SurveyResultsDto> GetAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct) ?? throw new NotFoundException("Survey", surveyId);
        // Only the survey's own camera counts; reference twins from the other camera are left out (D-039).
        var images = (await _images.ListBySurveyAsync(surveyId, ct)).Where(i => ImagePairing.IsCounted(survey, i)).ToList();
        var tallies = await _detections.TallyBySurveyAsync(surveyId, ct);

        var confirmedByImage = tallies
            .Where(t => ReviewRules.IsConfirmed(t.Status))
            .GroupBy(t => t.ImageId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Count));

        int Sum(Func<DetectionTally, bool> match) => tallies.Where(match).Sum(t => t.Count);

        var photos = images
            .Select(i => new PhotoPointDto(i.Id, i.FileName, i.Latitude, i.Longitude, i.TakenAt,
                confirmedByImage.GetValueOrDefault(i.Id), i.Reviewed, i.Flagged))
            .ToList();

        var counts = new ResultCountsDto(
            Photos: images.Count,
            ReviewedPhotos: images.Count(i => i.Reviewed),
            FlaggedPhotos: images.Count(i => i.Flagged),
            PhotosWithPigs: photos.Count(p => p.Confirmed > 0),
            PhotosWithGps: photos.Count(p => p.Latitude is not null && p.Longitude is not null),
            Confirmed: Sum(t => ReviewRules.IsConfirmed(t.Status)),
            ConfirmedFromAi: Sum(t => ReviewRules.IsConfirmed(t.Status) && t.Source == DetectionSource.Ai),
            DrawnByRanger: Sum(t => ReviewRules.IsConfirmed(t.Status) && t.Source == DetectionSource.Ranger),
            RejectedAi: Sum(t => t.Status == DetectionStatus.Rejected && t.Source == DetectionSource.Ai),
            PendingAi: Sum(t => t.Status == DetectionStatus.Pending));

        var provisional = survey.Status != SurveyStatus.Reviewed;
        var provisionalReason = provisional ? ProvisionalReason(survey.Status, counts) : null;

        var (estimate, unavailable) = await EstimateAsync(survey, counts, provisional, ct);

        var notes = new List<Assumption>();
        if (counts.Photos > 1)
            notes.Add(Assumption.Warning(
                "Pigs seen in overlapping photos may be counted twice. Check overlapping photos before relying on the total."));
        if (survey.AreaKm2 is null)
            notes.Add(Assumption.Info("Add the survey area (km²) to the survey to see pigs per km²."));

        return new SurveyResultsDto(survey.Id, survey.Status, provisional, provisionalReason, counts, estimate, unavailable,
            photos, notes);
    }

    private async Task<(EstimateResponse? Estimate, string? Unavailable)> EstimateAsync(
        Survey survey, ResultCountsDto counts, bool provisional, CancellationToken ct)
    {
        var timeNote = SurveyConditions.MatchTime(survey.StartTime).Note;
        if (survey.TimeWindow is null)
            return (null, timeNote?.Message ?? "This flight time is outside the study's survey windows, so no estimate can be made.");

        var errors = new ValidationErrors();
        var state = await _parameters.GetAsync(ct);
        var detection = ConditionsResolver.Compute(DetectionModelKind.TimeSeason, survey.Camera, survey.TimeWindow,
            survey.Season, null, null, errors, state.Parameters);
        if (detection is null)
            return (null, "The research model couldn't be applied to this survey's conditions.");

        var e = PopulationEstimator.Estimate(counts.Confirmed, detection.Probability, survey.AreaKm2);

        var assumptions = ModelParameterNotes.For(state)
            .Concat(timeNote is null ? Array.Empty<Assumption>() : new[] { timeNote })
            .Append(Assumption.Info(
                $"Season: {survey.Season.ToString().ToLowerInvariant()}, camera: {survey.Camera.ToString().ToLowerInvariant()}, from the survey details."))
            .Concat(detection.Assumptions)
            .Concat(Hvala2023Coefficients.StudyConditions.Select(Assumption.Info))
            .Append(Assumption.Warning("The model was fitted on feral pigs at one NT site. Other species or very different habitats are estimates only."))
            .ToList();

        return (new EstimateResponse(
            e.ConfirmedCount,
            Math.Round(e.DetectionProbability, 4),
            Math.Round(e.Estimate, 2),
            e.EstimateRounded,
            e.Low,
            e.High,
            e.DensityPerKm2,
            e.Method,
            detection.Model,
            detection.ModelVersion,
            survey.TimeWindow,
            survey.Season,
            provisional,
            assumptions), null);
    }

    private static string ProvisionalReason(SurveyStatus status, ResultCountsDto c)
    {
        if (status is SurveyStatus.Draft or SurveyStatus.Uploading or SurveyStatus.Processing)
            return "The review hasn't started. These numbers will change as photos are checked.";
        var parts = new List<string> { $"{c.ReviewedPhotos} of {c.Photos} photos checked" };
        if (c.PendingAi > 0) parts.Add($"{c.PendingAi} AI suggestion{(c.PendingAi == 1 ? "" : "s")} still to decide");
        return $"Provisional until the review is complete: {string.Join(", ", parts)}.";
    }
}
