using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.Review;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.TwinCheck.Models;
using AerialWatch.Contracts.Wfp.TwinCheck.Services;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.TwinCheck;

/// <summary>
/// Checking pigs on the other camera's photo, both ways (D-040, D-041).
/// <list type="bullet">
/// <item>Counted → twin: every accepted box on a counted photo gets a suggested position on the twin; the ranger confirms
/// it, moves it, or says it isn't visible there.</item>
/// <item>Twin → counted: a pig drawn first on the twin (a "sighting") gets a suggested position on the counted photo;
/// confirming it creates the counted pig, or the ranger says it isn't visible on the counted photo.</item>
/// </list>
/// Suggestions come from TwinMapping, fitted to the pairs rangers already placed in this survey. Only detections on the
/// survey's own camera are ever counted.
/// </summary>
public sealed class TwinCheckService : ITwinCheckService
{
    private readonly ISurveyRepository _surveys;
    private readonly ISurveyImageRepository _images;
    private readonly IDetectionRepository _detections;
    private readonly ITwinMarkRepository _marks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public TwinCheckService(ISurveyRepository surveys, ISurveyImageRepository images, IDetectionRepository detections,
        ITwinMarkRepository marks, IUnitOfWork unitOfWork, IClock clock)
    {
        _surveys = surveys;
        _images = images;
        _detections = detections;
        _marks = marks;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<TwinCheckDto> GetAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await _images.GetByIdAsync(imageId, ct) ?? throw new NotFoundException("Photo", imageId);
        var survey = await GetSurveyAsync(image.SurveyId, ct);
        var twin = ImagePairing.IsCounted(survey, image) ? await FindTwinAsync(image, ct) : null;
        if (twin is null)
            return new TwinCheckDto(image.Id, null, 0, Array.Empty<TwinMarkDto>(), Array.Empty<TwinSightingDto>());

        var detections = await _detections.ListByImageAsync(image.Id, ct);
        var all = await _marks.ListByImageAsync(image.Id, ct);
        var byDetection = all.Where(m => m.DetectionId is not null).ToDictionary(m => m.DetectionId!.Value);
        var mapping = await FitAsync(survey.Id, image, twin, ct);

        var marks = new List<TwinMarkDto>();
        for (var i = 0; i < detections.Count; i++)
        {
            var d = detections[i];
            if (!ReviewRules.IsConfirmed(d.Status)) continue;
            var number = i + 1; // same numbering as the box labels in the review viewer

            if (byDetection.TryGetValue(d.Id, out var mark))
            {
                marks.Add(mark.Status == TwinMarkStatus.NotVisible
                    ? new TwinMarkDto(d.Id, number, TwinMarkState.NotVisible, null, null, null, null)
                    : new TwinMarkDto(d.Id, number, TwinMarkState.Confirmed, mark.X, mark.Y, mark.Width, mark.Height));
                continue;
            }

            var guess = mapping is null || twin.WidthPx is not { } tw || twin.HeightPx is not { } th
                ? (PixelBox?)null
                : mapping.Map(new PixelBox(d.X, d.Y, d.Width, d.Height), tw, th);
            marks.Add(new TwinMarkDto(d.Id, number, TwinMarkState.Suggested, guess?.X, guess?.Y, guess?.Width, guess?.Height));
        }

        var sightings = all.Where(m => m.DetectionId is null).OrderBy(m => m.CreatedAt)
            .Select((m, i) => ToSighting(m, i, twin, image, mapping)).ToList();
        return new TwinCheckDto(image.Id, twin.Id, mapping?.LearnedFrom ?? 0, marks, sightings);
    }

    // ---- counted pig → twin ----

    public async Task<TwinMarkDto> SetAsync(Guid detectionId, SetTwinMarkRequest request, string? user, CancellationToken ct = default)
    {
        var (detection, image, _, twin) = await GetEditableAsync(detectionId, ct);

        if (request.State == TwinMarkState.Suggested || !Enum.IsDefined(request.State))
            throw new RequestValidationException("state", "Choose confirmed or not visible.");

        if (request.State == TwinMarkState.Confirmed)
        {
            if (request.X is not { } x || request.Y is not { } y || request.Width is not { } w || request.Height is not { } h)
                throw new RequestValidationException("box", "Send x, y, width and height to place the pig on the other photo.");
            ValidateBox(new PixelBox(x, y, w, h), twin, "the other photo");
        }

        var now = _clock.UtcNow;
        var mark = await _marks.GetByDetectionAsync(detection.Id, ct);
        if (mark is null)
        {
            mark = new TwinMark { DetectionId = detection.Id, CreatedAt = now };
            await _marks.AddAsync(mark, ct);
        }
        mark.CountedImageId = image.Id;
        mark.TwinImageId = twin.Id;
        mark.Status = request.State == TwinMarkState.NotVisible ? TwinMarkStatus.NotVisible : TwinMarkStatus.Confirmed;
        var visible = mark.Status == TwinMarkStatus.Confirmed;
        mark.X = visible ? request.X : null;
        mark.Y = visible ? request.Y : null;
        mark.Width = visible ? request.Width : null;
        mark.Height = visible ? request.Height : null;
        Touch(mark, user, now);
        await _unitOfWork.SaveChangesAsync(ct);

        return new TwinMarkDto(detection.Id, await NumberOfAsync(image.Id, detection.Id, ct), request.State, mark.X, mark.Y, mark.Width, mark.Height);
    }

    public async Task ClearAsync(Guid detectionId, CancellationToken ct = default)
    {
        _ = await GetEditableAsync(detectionId, ct);
        var mark = await _marks.GetByDetectionAsync(detectionId, ct);
        if (mark is null) return;
        _marks.Remove(mark);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ---- twin → counted photo ----

    public async Task<TwinSightingDto> AddSightingAsync(Guid imageId, TwinBoxRequest request, string? user, CancellationToken ct = default)
    {
        var image = await _images.GetByIdAsync(imageId, ct) ?? throw new NotFoundException("Photo", imageId);
        var survey = await GetSurveyAsync(image.SurveyId, ct);
        var twin = await GetEditableTwinAsync(survey, image, ct);
        var box = new PixelBox(request.X, request.Y, request.Width, request.Height);
        ValidateBox(box, twin, "the other photo");

        var now = _clock.UtcNow;
        var mark = new TwinMark
        {
            DetectionId = null,
            CountedImageId = image.Id,
            TwinImageId = twin.Id,
            Status = TwinMarkStatus.Confirmed,
            X = box.X, Y = box.Y, Width = box.Width, Height = box.Height,
            CreatedAt = now,
        };
        Touch(mark, user, now);
        await _marks.AddAsync(mark, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await SightingDtoAsync(survey, image, twin, mark, ct);
    }

    public async Task<TwinMarkDto> ConfirmSightingAsync(Guid markId, TwinBoxRequest request, string? user, CancellationToken ct = default)
    {
        var (mark, image, _, _) = await GetEditableSightingAsync(markId, ct);
        var box = new PixelBox(request.X, request.Y, request.Width, request.Height);
        ValidateBox(box, image, "the photo");

        var now = _clock.UtcNow;
        var who = CleanUser(user);
        var detection = new AnimalDetection
        {
            ImageId = image.Id,
            X = box.X, Y = box.Y, Width = box.Width, Height = box.Height,
            Label = AnimalDetection.PigLabel,
            Confidence = null,
            Source = DetectionSource.Ranger,
            Status = DetectionStatus.Accepted, // the ranger confirmed it on the counted photo
            ReviewedBy = who,
            ReviewedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _detections.AddAsync(detection, ct);
        mark.DetectionId = detection.Id;
        mark.NotVisibleOnCounted = false;
        Touch(mark, user, now);
        await _unitOfWork.SaveChangesAsync(ct);

        return new TwinMarkDto(detection.Id, await NumberOfAsync(image.Id, detection.Id, ct), TwinMarkState.Confirmed,
            mark.X, mark.Y, mark.Width, mark.Height);
    }

    public async Task<TwinSightingDto> SetSightingVisibilityAsync(Guid markId, SetSightingVisibilityRequest request, string? user, CancellationToken ct = default)
    {
        var (mark, image, survey, twin) = await GetEditableSightingAsync(markId, ct);
        mark.NotVisibleOnCounted = request.NotVisibleOnCounted;
        Touch(mark, user, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
        return await SightingDtoAsync(survey, image, twin, mark, ct);
    }

    public async Task RemoveSightingAsync(Guid markId, CancellationToken ct = default)
    {
        var (mark, _, _, _) = await GetEditableSightingAsync(markId, ct);
        _marks.Remove(mark);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ---- summary ----

    public async Task<TwinCheckSummaryDto> GetSummaryAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        var images = await _images.ListBySurveyAsync(surveyId, ct);
        var twins = ImagePairing.Twins(images);
        var countedWithTwin = images.Where(i => ImagePairing.IsCounted(survey, i) && twins.ContainsKey(i.Id)).Select(i => i.Id).ToHashSet();
        if (countedWithTwin.Count == 0) return new TwinCheckSummaryDto(null, 0, 0, 0, 0, 0);

        var pigs = (await _detections.TallyBySurveyAsync(surveyId, ct))
            .Where(t => countedWithTwin.Contains(t.ImageId) && ReviewRules.IsConfirmed(t.Status)).Sum(t => t.Count);
        var all = (await _marks.ListBySurveyAsync(surveyId, ct)).Where(m => countedWithTwin.Contains(m.CountedImageId)).ToList();
        var checkedPigs = all.Where(m => m.Detection is { } d && ReviewRules.IsConfirmed(d.Status)).ToList();

        return new TwinCheckSummaryDto(
            ImagePairing.Other(survey.Camera),
            pigs,
            checkedPigs.Count,
            checkedPigs.Count(m => m.Status == TwinMarkStatus.Confirmed),
            checkedPigs.Count(m => m.Status == TwinMarkStatus.NotVisible),
            all.Count(m => m.DetectionId is null && m.NotVisibleOnCounted));
    }

    // ---- helpers ----

    private async Task<Survey> GetSurveyAsync(Guid surveyId, CancellationToken ct) =>
        await _surveys.GetByIdAsync(surveyId, ct) ?? throw new NotFoundException("Survey", surveyId);

    private async Task<SurveyImage?> FindTwinAsync(SurveyImage image, CancellationToken ct) =>
        image.PairKey is { } key
            ? (await _images.ListByPairKeyAsync(image.SurveyId, key, ct)).FirstOrDefault(i => i.Id != image.Id && i.Camera != image.Camera)
            : null;

    /// <summary>The twin of a counted photo in a survey whose review is open.</summary>
    private async Task<SurveyImage> GetEditableTwinAsync(Survey survey, SurveyImage image, CancellationToken ct)
    {
        if (!ReviewRules.AcceptsChanges(survey.Status))
            throw new RequestValidationException("survey", ReviewRules.NotEditableMessage(survey.Status));
        var twin = ImagePairing.IsCounted(survey, image) ? await FindTwinAsync(image, ct) : null;
        return twin ?? throw new RequestValidationException("image", "This photo has no twin from the other camera.");
    }

    private async Task<(AnimalDetection Detection, SurveyImage Image, Survey Survey, SurveyImage Twin)> GetEditableAsync(Guid detectionId, CancellationToken ct)
    {
        var detection = await _detections.GetByIdAsync(detectionId, ct) ?? throw new NotFoundException("Box", detectionId);
        var image = await _images.GetByIdAsync(detection.ImageId, ct) ?? throw new NotFoundException("Photo", detection.ImageId);
        var survey = await GetSurveyAsync(image.SurveyId, ct);
        if (!ReviewRules.AcceptsChanges(survey.Status))
            throw new RequestValidationException("survey", ReviewRules.NotEditableMessage(survey.Status));
        if (!ReviewRules.IsConfirmed(detection.Status))
            throw new RequestValidationException("box", "Accept the box first; only confirmed pigs are checked on the other photo.");
        var twin = await GetEditableTwinAsync(survey, image, ct);
        return (detection, image, survey, twin);
    }

    private async Task<(TwinMark Mark, SurveyImage Image, Survey Survey, SurveyImage Twin)> GetEditableSightingAsync(Guid markId, CancellationToken ct)
    {
        var mark = await _marks.GetByIdAsync(markId, ct) ?? throw new NotFoundException("Sighting", markId);
        if (mark.DetectionId is not null)
            throw new RequestValidationException("sighting", "This pig is already on the counted photo; change it there.");
        var image = await _images.GetByIdAsync(mark.CountedImageId, ct) ?? throw new NotFoundException("Photo", mark.CountedImageId);
        var survey = await GetSurveyAsync(image.SurveyId, ct);
        var twin = await GetEditableTwinAsync(survey, image, ct);
        return (mark, image, survey, twin);
    }

    private static void ValidateBox(PixelBox b, SurveyImage photo, string where)
    {
        var tol = ReviewRules.EdgeTolerancePx;
        if (new[] { b.X, b.Y, b.Width, b.Height }.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            throw new RequestValidationException("box", "The box position must be a number.");
        if (b.Width < ReviewRules.MinBoxSizePx || b.Height < ReviewRules.MinBoxSizePx)
            throw new RequestValidationException("box", $"A box must be at least {ReviewRules.MinBoxSizePx:0} × {ReviewRules.MinBoxSizePx:0} pixels.");
        if (b.X < -tol || b.Y < -tol || (photo.WidthPx is { } w && b.X + b.Width > w + tol) || (photo.HeightPx is { } h && b.Y + b.Height > h + tol))
            throw new RequestValidationException("box", $"The box must be inside {where}.");
    }

    private async Task<TwinSightingDto> SightingDtoAsync(Survey survey, SurveyImage image, SurveyImage twin, TwinMark mark, CancellationToken ct)
    {
        var mapping = await FitAsync(survey.Id, image, twin, ct);
        var index = (await _marks.ListByImageAsync(image.Id, ct)).Where(m => m.DetectionId is null).OrderBy(m => m.CreatedAt)
            .Select((m, i) => (m.Id, i)).FirstOrDefault(t => t.Id == mark.Id).i;
        return ToSighting(mark, index, twin, image, mapping);
    }

    private static TwinSightingDto ToSighting(TwinMark m, int index, SurveyImage twin, SurveyImage image, TwinMapping? mapping)
    {
        var twinBox = new PixelBox(m.X ?? 0, m.Y ?? 0, m.Width ?? 0, m.Height ?? 0);
        var guess = mapping is null || image.WidthPx is not { } w || image.HeightPx is not { } h
            ? (PixelBox?)null
            : mapping.MapBack(twinBox, w, h);
        var prefix = twin.Camera == CameraType.Rgb ? "c" : "t";
        return new TwinSightingDto(m.Id, $"{prefix}{index + 1}",
            m.NotVisibleOnCounted ? TwinSightingState.NotVisibleOnCounted : TwinSightingState.SuggestedOnCounted,
            twinBox.X, twinBox.Y, twinBox.Width, twinBox.Height, guess?.X, guess?.Y, guess?.Width, guess?.Height);
    }

    private async Task<int> NumberOfAsync(Guid imageId, Guid detectionId, CancellationToken ct) =>
        (await _detections.ListByImageAsync(imageId, ct)).Select((d, i) => (d.Id, i)).FirstOrDefault(t => t.Id == detectionId).i + 1;

    /// <summary>Mapping fitted to the pairs already placed in this survey; null when photo sizes are unknown.</summary>
    private async Task<TwinMapping?> FitAsync(Guid surveyId, SurveyImage image, SurveyImage twin, CancellationToken ct)
    {
        if (image.WidthPx is not { } sw || image.HeightPx is not { } sh || twin.WidthPx is not { } tw || twin.HeightPx is not { } th)
            return null;
        var points = (await _marks.ListBySurveyAsync(surveyId, ct))
            .Where(m => m.Status == TwinMarkStatus.Confirmed && m.Detection is not null
                && m.X is not null && m.Y is not null && m.Width is not null && m.Height is not null)
            .Select(m => new MatchedBoxes(
                new PixelBox(m.Detection!.X, m.Detection.Y, m.Detection.Width, m.Detection.Height),
                new PixelBox(m.X!.Value, m.Y!.Value, m.Width!.Value, m.Height!.Value)))
            .ToList();
        return TwinMapping.Fit(points, (double)tw / sw, (double)th / sh);
    }

    private static void Touch(TwinMark mark, string? user, DateTimeOffset now)
    {
        mark.ReviewedBy = CleanUser(user);
        mark.ReviewedAt = now;
        mark.UpdatedAt = now;
    }

    private static string? CleanUser(string? user)
    {
        if (string.IsNullOrWhiteSpace(user)) return null;
        var t = user.Trim();
        return t.Length <= AnimalDetection.ReviewedByMaxLength ? t : t[..AnimalDetection.ReviewedByMaxLength];
    }
}
