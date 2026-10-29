using System.Globalization;
using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.Common;
using AerialWatch.Application.Wfp.ModelParameters;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Surveys.Models;
using AerialWatch.Contracts.Wfp.Surveys.Services;
using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Surveys;

/// <summary>Survey use cases: validation, deriving study conditions, persistence through repositories.</summary>
public sealed class SurveyService : ISurveyService
{
    private const double StudyAltitudeM = 52;

    private readonly ISurveyRepository _surveys;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IActiveModelParameters _parameters;
    private readonly IFileStorage _storage;

    public SurveyService(ISurveyRepository surveys, IUnitOfWork unitOfWork, IClock clock, IActiveModelParameters parameters,
        IFileStorage storage)
    {
        _surveys = surveys;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _parameters = parameters;
        _storage = storage;
    }

    public const int MaxPageSize = 100;

    private static readonly Dictionary<string, SurveySortField> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = SurveySortField.Name,
        ["siteName"] = SurveySortField.SiteName,
        ["surveyDate"] = SurveySortField.SurveyDate,
        ["status"] = SurveySortField.Status,
        ["imageCount"] = SurveySortField.ImageCount,
        ["createdAt"] = SurveySortField.CreatedAt,
    };

    public async Task<PagedResult<SurveySummaryDto>> ListAsync(SurveyListQuery query, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        if (query.Page < 1) errors.Add("page", "Page must be 1 or more.");
        if (query.PageSize is < 1 or > MaxPageSize) errors.Add("pageSize", $"Page size must be between 1 and {MaxPageSize}.");

        DateOnly? date = null;
        if (!string.IsNullOrWhiteSpace(query.Date))
        {
            if (ConditionsResolver.TryParseDate(query.Date, out var d)) date = d;
            else errors.Add("date", "Date must be in yyyy-MM-dd format.");
        }

        // Default: newest first. A sort column without a direction sorts ascending.
        var sort = SurveySortField.CreatedAt;
        var descending = string.IsNullOrWhiteSpace(query.Sort);
        if (!string.IsNullOrWhiteSpace(query.Sort) && !SortFields.TryGetValue(query.Sort.Trim(), out sort))
            errors.Add("sort", $"Sort must be one of: {string.Join(", ", SortFields.Keys)}.");
        if (!string.IsNullOrWhiteSpace(query.Dir))
        {
            if (query.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase)) descending = true;
            else if (query.Dir.Equals("asc", StringComparison.OrdinalIgnoreCase)) descending = false;
            else errors.Add("dir", "Dir must be asc or desc.");
        }
        errors.ThrowIfAny();

        var text = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var result = await _surveys.SearchAsync(new SurveySearch(
            text, date, query.Camera, query.Status, sort, descending,
            Skip: (query.Page - 1) * query.PageSize, Take: query.PageSize), ct);

        return new PagedResult<SurveySummaryDto>(
            result.Items.Select(i => ToSummary(i.Survey, i.ImageCount)).ToList(), query.Page, query.PageSize, result.Total);
    }

    public async Task<SurveyStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var stats = await _surveys.GetStatsAsync(ct);
        return new SurveyStatsDto(stats.Surveys, stats.AwaitingReview, stats.Photos);
    }

    public async Task<SurveyDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(id, ct) ?? throw new NotFoundException("Survey", id);
        var imageCount = await _surveys.CountImagesAsync(id, ct);
        return ToDetail(survey, imageCount, await _parameters.GetAsync(ct));
    }

    public async Task<SurveyDetailDto> CreateAsync(CreateSurveyRequest request, CancellationToken ct = default)
    {
        var errors = new ValidationErrors();
        var name = request.Name?.Trim() ?? string.Empty;
        var site = request.SiteName?.Trim() ?? string.Empty;

        if (name.Length == 0) errors.Add("name", "Survey name is required.");
        else if (name.Length > Survey.NameMaxLength) errors.Add("name", $"Survey name must be {Survey.NameMaxLength} characters or fewer.");

        if (site.Length == 0) errors.Add("siteName", "Site / area is required.");
        else if (site.Length > Survey.SiteNameMaxLength) errors.Add("siteName", $"Site must be {Survey.SiteNameMaxLength} characters or fewer.");

        if (!ConditionsResolver.TryParseDate(request.SurveyDate, out var date)) errors.Add("surveyDate", "Date must be in yyyy-MM-dd format.");
        if (!ConditionsResolver.TryParseTime(request.StartTime, out var time)) errors.Add("startTime", "Time must be in HH:mm format.");
        if (request.AltitudeM is <= 0 or > 500) errors.Add("altitudeM", "Altitude must be between 1 and 500 m.");
        if (request.AreaKm2 is <= 0) errors.Add("areaKm2", "Area must be greater than 0.");
        if (request.Notes is { Length: > Survey.NotesMaxLength }) errors.Add("notes", $"Notes must be {Survey.NotesMaxLength} characters or fewer.");
        errors.ThrowIfAny();

        var now = _clock.UtcNow;
        var survey = new Survey
        {
            Name = name,
            SiteName = site,
            SurveyDate = date,
            StartTime = time,
            Season = request.Season ?? SurveyConditions.SeasonFromDate(date),
            Camera = request.Camera,
            TimeWindow = SurveyConditions.MatchTime(time).Window,
            AltitudeM = request.AltitudeM,
            AreaKm2 = request.AreaKm2,
            GroundCover = request.GroundCover,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = SurveyStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _surveys.AddAsync(survey, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDetail(survey, imageCount: 0, await _parameters.GetAsync(ct));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(id, ct) ?? throw new NotFoundException("Survey", id);
        _surveys.Remove(survey);
        await _unitOfWork.SaveChangesAsync(ct); // photo and detection rows go by cascade delete

        // The database doesn't know about files: remove the survey's photo folder after the rows are gone (D-030).
        try { await _storage.DeleteFolderAsync(SurveyImageStorageKeys.SurveyFolder(id), ct); }
        catch (IOException) { /* an orphan folder is harmless; the survey is already deleted */ }
        catch (UnauthorizedAccessException) { }
    }

    // ---- mapping ----

    private static string Date(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Time(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static SurveySummaryDto ToSummary(Survey s, int imageCount) => new(
        s.Id, s.Name, s.SiteName, Date(s.SurveyDate), Time(s.StartTime), s.Camera, s.Season, s.TimeWindow,
        s.Status, imageCount, s.CreatedAt);

    private static SurveyDetailDto ToDetail(Survey s, int imageCount, ModelParametersState state)
    {
        var assumptions = new List<Assumption>(ModelParameterNotes.For(state));
        var match = SurveyConditions.MatchTime(s.StartTime);
        if (match.Note is not null) assumptions.Add(match.Note);

        double? p = null;
        if (s.TimeWindow is { } window)
        {
            var result = DetectionProbabilityModel.TimeSeason(window, s.Season, s.Camera, state.Parameters);
            p = Math.Round(result.Probability, 4);
            assumptions.AddRange(result.Assumptions);
        }

        if (s.AltitudeM is { } alt && Math.Abs(alt - StudyAltitudeM) > 20)
            assumptions.Add(Assumption.Warning($"Flight altitude {alt:0} m differs from the study's ~52 m; detection may differ."));

        if (s.GroundCover == GroundCover.Dense)
            assumptions.Add(Assumption.Warning("Dense canopy: pigs under trees are much harder to see than in the study's average conditions."));

        assumptions.AddRange(Hvala2023Coefficients.StudyConditions.Select(Assumption.Info));

        return new SurveyDetailDto(
            s.Id, s.Name, s.SiteName, Date(s.SurveyDate), Time(s.StartTime), s.Camera, s.Season, s.TimeWindow,
            s.Status, imageCount, s.CreatedAt, s.AltitudeM, s.AreaKm2, s.GroundCover, s.Notes, p, assumptions,
            s.TrainingSplit);
    }
}
