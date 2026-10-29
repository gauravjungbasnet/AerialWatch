using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.SurveyImages;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.Review.Models;
using AerialWatch.Contracts.Wfp.Review.Services;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Review;

/// <summary>
/// Ranger review (D-034). Every box keeps its source (AI or ranger) and status, so the confirmed count, the AI's
/// accuracy and the audit trail all come from the same rows. AI suggestions are never deleted, only rejected.
/// Only photos from the survey's own camera are reviewed and counted; the other camera's photos are reference twins,
/// returned with the photo they belong to and never editable (D-039).
/// </summary>
public sealed class ReviewService : IReviewService
{
    private readonly ISurveyRepository _surveys;
    private readonly ISurveyImageRepository _images;
    private readonly IDetectionRepository _detections;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReviewService(ISurveyRepository surveys, ISurveyImageRepository images, IDetectionRepository detections,
        IUnitOfWork unitOfWork, IClock clock)
    {
        _surveys = surveys;
        _images = images;
        _detections = detections;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    // ---- survey ----

    public async Task<ReviewSummaryDto> StartAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        if (survey.Status == SurveyStatus.InReview) return await GetSummaryAsync(surveyId, ct);

        if (!ReviewRules.CanStart(survey.Status))
            throw new RequestValidationException("survey", survey.Status == SurveyStatus.Reviewed
                ? "This review is already complete. Reopen it to make changes."
                : survey.Status == SurveyStatus.Draft
                    ? "Upload photos before starting the review."
                    : $"Review can't start while the survey is {survey.Status}.");
        if (Counted(survey, await _images.ListBySurveyAsync(surveyId, ct)).Count == 0)
            throw new RequestValidationException("survey",
                $"Upload {ImagePairing.Label(survey.Camera)} photos before starting the review (only reference photos from the other camera are here).");

        survey.Status = SurveyStatus.InReview;
        survey.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return await GetSummaryAsync(surveyId, ct);
    }

    public async Task<ReviewSummaryDto> GetSummaryAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        var images = Counted(survey, await _images.ListBySurveyAsync(surveyId, ct));
        var tallies = await _detections.TallyBySurveyAsync(surveyId, ct);
        return Summarise(survey, images, tallies);
    }

    public async Task<ReviewSummaryDto> CompleteSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        if (survey.Status == SurveyStatus.Reviewed) return await GetSummaryAsync(surveyId, ct);
        EnsureEditable(survey);

        var summary = Summarise(survey, Counted(survey, await _images.ListBySurveyAsync(surveyId, ct)), await _detections.TallyBySurveyAsync(surveyId, ct));
        if (!summary.CanComplete) throw new RequestValidationException("survey", summary.BlockedReason!);

        survey.Status = SurveyStatus.Reviewed;
        survey.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return summary with { Status = SurveyStatus.Reviewed, CanComplete = false, BlockedReason = null };
    }

    public async Task<ReviewSummaryDto> ReopenSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        if (survey.Status != SurveyStatus.Reviewed)
            throw new RequestValidationException("survey", "Only a completed review can be reopened.");

        survey.Status = SurveyStatus.InReview;
        survey.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return await GetSummaryAsync(surveyId, ct);
    }

    public async Task<BulkAcceptResponse> BulkAcceptAsync(Guid surveyId, BulkAcceptRequest request, string? user, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        if (double.IsNaN(request.MinConfidence) || request.MinConfidence is < 0 or > 1)
            throw new RequestValidationException("minConfidence", "Minimum confidence must be between 0 and 1.");
        EnsureEditable(survey);

        var now = _clock.UtcNow;
        var who = CleanUser(user);
        var pending = await _detections.ListPendingAiAsync(surveyId, request.MinConfidence, ct);
        foreach (var d in pending) MarkReviewed(d, DetectionStatus.Accepted, who, now);
        if (pending.Count > 0) await _unitOfWork.SaveChangesAsync(ct);

        return new BulkAcceptResponse(pending.Count, await GetSummaryAsync(surveyId, ct));
    }

    // ---- photos ----

    public async Task<IReadOnlyList<ReviewImageDto>> ListImagesAsync(Guid surveyId, ReviewImageFilter filter, CancellationToken ct = default)
    {
        var survey = await GetSurveyAsync(surveyId, ct);
        var images = Counted(survey, await _images.ListBySurveyAsync(surveyId, ct));
        var counts = CountsByImage(await _detections.TallyBySurveyAsync(surveyId, ct));

        return images
            .Select(i => ToImageDto(i, counts.GetValueOrDefault(i.Id)))
            .Where(i => filter switch
            {
                ReviewImageFilter.NotReviewed => !i.Reviewed,
                ReviewImageFilter.Pending => i.Pending > 0,
                ReviewImageFilter.WithPigs => i.Confirmed > 0,
                ReviewImageFilter.NoPigs => i.Reviewed && i.Confirmed == 0,
                ReviewImageFilter.Flagged => i.Flagged,
                _ => true,
            })
            .ToList();
    }

    public async Task<ReviewImageDetailDto> GetImageAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await GetImageEntityAsync(imageId, ct);
        var detections = await _detections.ListByImageAsync(imageId, ct);

        TwinImageDto? twin = null;
        if (image.PairKey is { } key)
        {
            var other = (await _images.ListByPairKeyAsync(image.SurveyId, key, ct)).FirstOrDefault(i => i.Id != image.Id && i.Camera != image.Camera);
            if (other is not null)
                twin = new TwinImageDto(other.Id, other.FileName, other.Camera, other.WidthPx, other.HeightPx, other.TakenAt, other.ThumbnailPath is not null);
        }
        return new ReviewImageDetailDto(ToImageDto(image, Count(detections)), detections.Select(ToDto).ToList(), twin);
    }

    public async Task<ReviewImageDto> CompleteImageAsync(Guid imageId, string? user, CancellationToken ct = default)
    {
        var (image, _) = await GetEditableImageAsync(imageId, ct);
        var detections = await _detections.ListByImageAsync(imageId, ct);
        var pending = detections.Count(d => d.Status == DetectionStatus.Pending);
        if (pending > 0)
            throw new RequestValidationException("image",
                $"Accept or reject the {pending} suggested box{(pending == 1 ? "" : "es")} on this photo first.");

        var now = _clock.UtcNow;
        image.Reviewed = true;
        image.ReviewedBy = CleanUser(user);
        image.ReviewedAt = now;
        image.UpdatedAt = now;
        await _unitOfWork.SaveChangesAsync(ct);
        return ToImageDto(image, Count(detections));
    }

    public async Task<ReviewImageDto> ReopenImageAsync(Guid imageId, CancellationToken ct = default)
    {
        var (image, _) = await GetEditableImageAsync(imageId, ct);
        image.Reviewed = false;
        image.ReviewedBy = null;
        image.ReviewedAt = null;
        image.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return ToImageDto(image, Count(await _detections.ListByImageAsync(imageId, ct)));
    }

    public async Task<ReviewImageDto> FlagImageAsync(Guid imageId, FlagImageRequest request, CancellationToken ct = default)
    {
        var (image, _) = await GetEditableImageAsync(imageId, ct);
        image.Flagged = request.Flagged;
        image.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return ToImageDto(image, Count(await _detections.ListByImageAsync(imageId, ct)));
    }

    // ---- boxes ----

    public async Task<DetectionDto> AddDetectionAsync(Guid imageId, AddDetectionRequest request, string? user, CancellationToken ct = default)
    {
        var (image, _) = await GetEditableImageAsync(imageId, ct);
        ValidateBox(request.X, request.Y, request.Width, request.Height, image);

        var now = _clock.UtcNow;
        var detection = new AnimalDetection
        {
            ImageId = image.Id,
            X = request.X,
            Y = request.Y,
            Width = request.Width,
            Height = request.Height,
            Label = AnimalDetection.PigLabel,
            Confidence = null,
            Source = DetectionSource.Ranger,
            Status = DetectionStatus.Accepted, // a ranger drawing a box is confirming a pig
            ReviewedBy = CleanUser(user),
            ReviewedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _detections.AddAsync(detection, ct);
        image.UpdatedAt = now;
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(detection);
    }

    public async Task<DetectionDto> UpdateDetectionAsync(Guid detectionId, UpdateDetectionRequest request, string? user, CancellationToken ct = default)
    {
        var detection = await _detections.GetByIdAsync(detectionId, ct) ?? throw new NotFoundException("Box", detectionId);
        var (image, _) = await GetEditableImageAsync(detection.ImageId, ct);

        var geometry = new[] { request.X, request.Y, request.Width, request.Height };
        var hasGeometry = geometry.Any(v => v is not null);
        if (hasGeometry && geometry.Any(v => v is null))
            throw new RequestValidationException("box", "Send x, y, width and height together to move or resize a box.");
        if (!hasGeometry && request.Status is null)
            throw new RequestValidationException("box", "Nothing to change.");
        if (request.Status is { } s && !Enum.IsDefined(s))
            throw new RequestValidationException("status", "Status must be pending, accepted or rejected.");
        if (request.Status == DetectionStatus.Pending && detection.Source == DetectionSource.Ranger)
            throw new RequestValidationException("status", "Boxes you drew are always confirmed; delete the box instead.");

        if (hasGeometry)
        {
            ValidateBox(request.X!.Value, request.Y!.Value, request.Width!.Value, request.Height!.Value, image);
            detection.X = request.X.Value;
            detection.Y = request.Y.Value;
            detection.Width = request.Width.Value;
            detection.Height = request.Height.Value;
        }

        var now = _clock.UtcNow;
        if (request.Status is { } status)
        {
            if (status == DetectionStatus.Pending)
            {
                // Undo: the suggestion goes back to "not yet checked".
                detection.Status = DetectionStatus.Pending;
                detection.ReviewedBy = null;
                detection.ReviewedAt = null;
                detection.UpdatedAt = now;
            }
            else MarkReviewed(detection, status, CleanUser(user), now);
        }
        else
        {
            // Moving a box is a review action too: record who touched it.
            detection.ReviewedBy = CleanUser(user);
            detection.ReviewedAt = now;
            detection.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(detection);
    }

    public async Task DeleteDetectionAsync(Guid detectionId, CancellationToken ct = default)
    {
        var detection = await _detections.GetByIdAsync(detectionId, ct) ?? throw new NotFoundException("Box", detectionId);
        _ = await GetEditableImageAsync(detection.ImageId, ct);
        if (detection.Source == DetectionSource.Ai)
            throw new RequestValidationException("box",
                "AI suggestions can't be deleted; reject it instead, so the AI's accuracy can be measured.");

        _detections.Remove(detection);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ---- helpers ----

    private async Task<Survey> GetSurveyAsync(Guid surveyId, CancellationToken ct) =>
        await _surveys.GetByIdAsync(surveyId, ct) ?? throw new NotFoundException("Survey", surveyId);

    private async Task<SurveyImage> GetImageEntityAsync(Guid imageId, CancellationToken ct) =>
        await _images.GetByIdAsync(imageId, ct) ?? throw new NotFoundException("Photo", imageId);

    private async Task<(SurveyImage Image, Survey Survey)> GetEditableImageAsync(Guid imageId, CancellationToken ct)
    {
        var image = await GetImageEntityAsync(imageId, ct);
        var survey = await GetSurveyAsync(image.SurveyId, ct);
        EnsureEditable(survey);
        if (!ImagePairing.IsCounted(survey, image))
            throw new RequestValidationException("image",
                $"This is a {ImagePairing.Label(image.Camera)} reference photo. Boxes go on the {ImagePairing.Label(survey.Camera)} photo, which is the one counted.");
        return (image, survey);
    }

    /// <summary>Photos from the survey's own camera: the ones reviewed and counted (D-039).</summary>
    private static List<SurveyImage> Counted(Survey survey, IEnumerable<SurveyImage> images) =>
        images.Where(i => ImagePairing.IsCounted(survey, i)).ToList();

    private static void EnsureEditable(Survey survey)
    {
        if (!ReviewRules.AcceptsChanges(survey.Status))
            throw new RequestValidationException("survey", ReviewRules.NotEditableMessage(survey.Status));
    }

    private static void ValidateBox(double x, double y, double width, double height, SurveyImage image)
    {
        var errors = new ValidationErrors();
        if (new[] { x, y, width, height }.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
        {
            errors.Add("box", "The box position must be a number.");
            errors.ThrowIfAny();
        }

        var tol = ReviewRules.EdgeTolerancePx;
        if (width < ReviewRules.MinBoxSizePx || height < ReviewRules.MinBoxSizePx)
            errors.Add("box", $"A box must be at least {ReviewRules.MinBoxSizePx:0} × {ReviewRules.MinBoxSizePx:0} pixels.");
        if (x < -tol || y < -tol)
            errors.Add("box", "The box must start inside the photo.");
        if (image.WidthPx is { } w && x + width > w + tol)
            errors.Add("box", "The box goes past the right edge of the photo.");
        if (image.HeightPx is { } h && y + height > h + tol)
            errors.Add("box", "The box goes past the bottom edge of the photo.");
        errors.ThrowIfAny();
    }

    private static void MarkReviewed(AnimalDetection d, DetectionStatus status, string? who, DateTimeOffset now)
    {
        d.Status = status;
        d.ReviewedBy = who;
        d.ReviewedAt = now;
        d.UpdatedAt = now;
    }

    private static string? CleanUser(string? user)
    {
        if (string.IsNullOrWhiteSpace(user)) return null;
        var t = user.Trim();
        return t.Length <= AnimalDetection.ReviewedByMaxLength ? t : t[..AnimalDetection.ReviewedByMaxLength];
    }

    private sealed record ImageCounts(int Confirmed, int Pending, int Rejected);

    private static ImageCounts Count(IEnumerable<AnimalDetection> detections)
    {
        var list = detections as IReadOnlyCollection<AnimalDetection> ?? detections.ToList();
        return new ImageCounts(
            list.Count(d => ReviewRules.IsConfirmed(d.Status)),
            list.Count(d => d.Status == DetectionStatus.Pending),
            list.Count(d => d.Status == DetectionStatus.Rejected));
    }

    private static Dictionary<Guid, ImageCounts> CountsByImage(IEnumerable<DetectionTally> tallies) =>
        tallies.GroupBy(t => t.ImageId).ToDictionary(g => g.Key, g => new ImageCounts(
            g.Where(t => ReviewRules.IsConfirmed(t.Status)).Sum(t => t.Count),
            g.Where(t => t.Status == DetectionStatus.Pending).Sum(t => t.Count),
            g.Where(t => t.Status == DetectionStatus.Rejected).Sum(t => t.Count)));

    private static ReviewSummaryDto Summarise(Survey survey, IReadOnlyList<SurveyImage> images, IReadOnlyList<DetectionTally> tallies)
    {
        var counts = CountsByImage(tallies);
        var reviewed = images.Count(i => i.Reviewed);
        var pending = tallies.Where(t => t.Status == DetectionStatus.Pending).Sum(t => t.Count);

        string? blocked = null;
        if (survey.Status != SurveyStatus.InReview) blocked = ReviewRules.NotEditableMessage(survey.Status);
        else if (images.Count == 0) blocked = "There are no photos to review.";
        else if (pending > 0) blocked = $"{pending} AI suggestion{(pending == 1 ? " is" : "s are")} still waiting to be accepted or rejected.";
        else if (reviewed < images.Count)
        {
            var left = images.Count - reviewed;
            blocked = $"{left} photo{(left == 1 ? " is" : "s are")} not checked yet.";
        }

        return new ReviewSummaryDto(
            survey.Id,
            survey.Status,
            images.Count,
            reviewed,
            images.Count(i => i.Flagged),
            counts.Count(c => c.Value.Confirmed > 0),
            tallies.Where(t => ReviewRules.IsConfirmed(t.Status)).Sum(t => t.Count),
            pending,
            tallies.Where(t => t.Status == DetectionStatus.Rejected).Sum(t => t.Count),
            tallies.Where(t => t.Source == DetectionSource.Ai).Sum(t => t.Count),
            tallies.Where(t => t.Source == DetectionSource.Ranger).Sum(t => t.Count),
            CanComplete: blocked is null,
            BlockedReason: blocked);
    }

    private static ReviewImageDto ToImageDto(SurveyImage i, ImageCounts? c) => new(
        i.Id, i.FileName, i.WidthPx, i.HeightPx, i.TakenAt, i.Latitude, i.Longitude, i.ThumbnailPath is not null,
        i.Reviewed, i.Flagged, i.ReviewedBy, i.ReviewedAt, c?.Confirmed ?? 0, c?.Pending ?? 0, c?.Rejected ?? 0);

    private static DetectionDto ToDto(AnimalDetection d) => new(
        d.Id, d.ImageId, d.X, d.Y, d.Width, d.Height, d.Label, d.Confidence, d.Source, d.Status, d.ModelVersion,
        d.ReviewedBy, d.ReviewedAt);
}
