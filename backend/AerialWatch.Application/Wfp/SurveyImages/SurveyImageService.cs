using System.Security.Cryptography;
using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp.Surveys;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Wfp.SurveyImages.Models;
using AerialWatch.Contracts.Wfp.SurveyImages.Services;
using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.SurveyImages;

/// <summary>
/// Survey photo use cases (D-030). Per file: check extension and size → check the real content type from its first
/// bytes → SHA-256 duplicate check → save original → read size/EXIF (reject if it doesn't decode) → thumbnail.
/// Rows are committed once per batch; files written for a batch whose commit fails are deleted again.
/// After each batch, thermal + colour twins are matched by file name and GPS is copied to counted photos (D-039).
/// </summary>
public sealed class SurveyImageService : ISurveyImageService
{
    private readonly ISurveyRepository _surveys;
    private readonly ISurveyImageRepository _images;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _storage;
    private readonly IImageProcessor _processor;
    private readonly IClock _clock;

    public SurveyImageService(ISurveyRepository surveys, ISurveyImageRepository images, IUnitOfWork unitOfWork,
        IFileStorage storage, IImageProcessor processor, IClock clock)
    {
        _surveys = surveys;
        _images = images;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _processor = processor;
        _clock = clock;
    }

    public async Task<UploadImagesResponse> UploadAsync(Guid surveyId, IReadOnlyList<UploadedFile> files, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct) ?? throw new NotFoundException("Survey", surveyId);

        var errors = new ValidationErrors();
        // Once review has started only reference twins from the other camera may be added (D-040); checked per file below.
        var locked = !SurveyImageRules.AcceptsChanges(survey.Status);
        if (locked && !SurveyImageRules.AcceptsReferencePhotos(survey.Status))
            errors.Add("survey", $"Photos can only be added before review starts (survey is {survey.Status}).");
        if (files.Count == 0)
            errors.Add("files", "Choose at least one photo.");
        else if (files.Count > SurveyImageRules.MaxFilesPerRequest)
            errors.Add("files", $"Send at most {SurveyImageRules.MaxFilesPerRequest} photos per request.");
        errors.ThrowIfAny();

        var knownHashes = new HashSet<string>(await _images.ListHashesAsync(surveyId, ct), StringComparer.OrdinalIgnoreCase);
        var outcomes = new List<OneFile>(files.Count);
        var added = new List<SurveyImage>();
        var writtenKeys = new List<string>();
        var now = _clock.UtcNow;
        var twins = new Dictionary<Guid, SurveyImage>();

        try
        {
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var outcome = await UploadOneAsync(survey, file, knownHashes, writtenKeys, now, ct);
                outcomes.Add(outcome);
                if (outcome.Image is not null) added.Add(outcome.Image);
            }

            if (added.Count > 0)
            {
                var all = await PairAsync(survey, added, now, locked, ct);
                if (locked)
                {
                    // Review has started: a photo from the survey's own camera would change the count, so refuse it.
                    foreach (var refused in added.Where(i => ImagePairing.IsCounted(survey, i)).ToList())
                    {
                        await TryDeleteAsync(refused.StoragePath);
                        writtenKeys.Remove(refused.StoragePath);
                        if (refused.ThumbnailPath is { } thumb) { await TryDeleteAsync(thumb); writtenKeys.Remove(thumb); }
                        knownHashes.Remove(refused.Sha256);
                        added.Remove(refused);
                        all.Remove(refused);
                        var at = outcomes.FindIndex(o => ReferenceEquals(o.Image, refused));
                        outcomes[at] = new OneFile(new UploadFileResult(refused.FileName, UploadFileStatus.Rejected,
                            $"The review has started, so only {ImagePairing.Label(ImagePairing.Other(survey.Camera))} reference photos " +
                            $"can be added. This looks like a {ImagePairing.Label(survey.Camera)} photo.", null), null, Array.Empty<string>());
                    }
                }
                twins = ImagePairing.Twins(all);
                foreach (var image in added) await _images.AddAsync(image, ct);
                if (survey.Status == SurveyStatus.Draft) survey.Status = SurveyStatus.Uploading;
                survey.UpdatedAt = now;
                await _unitOfWork.SaveChangesAsync(ct); // also saves GPS copied onto existing photos
            }
        }
        catch
        {
            // Nothing was committed, so don't leave orphan files behind.
            foreach (var key in writtenKeys) await TryDeleteAsync(key);
            throw;
        }

        var results = outcomes.Select(o => o.Image is null
            ? o.Result
            : o.Result with { Message = Message(survey, o.Image, o.Warnings, twins), Image = ToDto(survey, o.Image, twins) }).ToList();

        var total = await _surveys.CountImagesAsync(surveyId, ct);
        return new UploadImagesResponse(
            surveyId,
            results.Count(r => r.Status == UploadFileStatus.Uploaded),
            results.Count(r => r.Status == UploadFileStatus.Duplicate),
            results.Count(r => r.Status == UploadFileStatus.Rejected),
            total,
            results);
    }

    public async Task<IReadOnlyList<SurveyImageDto>> ListAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct) ?? throw new NotFoundException("Survey", surveyId);
        var images = await _images.ListBySurveyAsync(surveyId, ct);
        var twins = ImagePairing.Twins(images);
        return images.Select(i => ToDto(survey, i, twins)).ToList();
    }

    public async Task<ImageFile> OpenAsync(Guid imageId, bool thumbnail, CancellationToken ct = default)
    {
        var image = await _images.GetByIdAsync(imageId, ct) ?? throw new NotFoundException("Photo", imageId);

        if (thumbnail && image.ThumbnailPath is { } thumbKey && await _storage.OpenReadAsync(thumbKey, ct) is { } thumb)
            return new ImageFile(thumb, SurveyImageRules.Jpeg, Path.ChangeExtension(image.FileName, ".jpg"));

        var original = await _storage.OpenReadAsync(image.StoragePath, ct)
            ?? throw new NotFoundException("Photo file", imageId);
        return new ImageFile(original, image.ContentType, image.FileName);
    }

    public async Task DeleteAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await _images.GetByIdAsync(imageId, ct) ?? throw new NotFoundException("Photo", imageId);
        var survey = await _surveys.GetByIdAsync(image.SurveyId, ct) ?? throw new NotFoundException("Survey", image.SurveyId);
        if (!SurveyImageRules.AcceptsChanges(survey.Status))
            throw new RequestValidationException("survey", $"Photos can only be removed before review starts (survey is {survey.Status}).");

        _images.Remove(image);
        survey.UpdatedAt = _clock.UtcNow;
        if (await _surveys.CountImagesAsync(survey.Id, ct) <= 1 && survey.Status == SurveyStatus.Uploading)
            survey.Status = SurveyStatus.Draft; // that was the last photo
        await _unitOfWork.SaveChangesAsync(ct);

        // Files go only after the row is gone, so a failed commit never leaves a row pointing at nothing.
        await TryDeleteAsync(image.StoragePath);
        if (image.ThumbnailPath is { } thumb) await TryDeleteAsync(thumb);
    }

    // ---- one file ----

    private sealed record OneFile(UploadFileResult Result, SurveyImage? Image, IReadOnlyList<string> Warnings);

    /// <summary>
    /// Matches the new photos with the survey's existing ones (D-039). Existing photos whose camera or location changes
    /// are loaded tracked and updated, so the change is saved with the batch. Returns all photos of the survey.
    /// </summary>
    private async Task<List<SurveyImage>> PairAsync(Survey survey, IReadOnlyList<SurveyImage> added, DateTimeOffset now, bool locked, CancellationToken ct)
    {
        var existing = await _images.ListBySurveyAsync(survey.Id, ct);
        var before = existing.ToDictionary(i => i.Id, i => (i.Camera, i.Latitude, i.Longitude, i.LocationFromTwin));
        var all = existing.Concat(added).ToList();
        ImagePairing.Resolve(survey, all);
        // During review the photos being counted are fixed: an existing photo never changes camera.
        if (locked)
            foreach (var image in existing) image.Camera = before[image.Id].Camera;

        foreach (var image in existing)
        {
            if (before[image.Id] == (image.Camera, image.Latitude, image.Longitude, image.LocationFromTwin)) continue;
            var tracked = await _images.GetByIdAsync(image.Id, ct);
            if (tracked is null) continue;
            tracked.Camera = image.Camera;
            tracked.Latitude = image.Latitude;
            tracked.Longitude = image.Longitude;
            tracked.LocationFromTwin = image.LocationFromTwin;
            tracked.UpdatedAt = now;
        }
        return all;
    }

    private static string? Message(Survey survey, SurveyImage image, IReadOnlyList<string> warnings, IReadOnlyDictionary<Guid, SurveyImage> twins)
    {
        var parts = new List<string>();
        var counted = ImagePairing.IsCounted(survey, image);
        if (!counted)
            parts.Add($"{Capitalise(ImagePairing.Label(image.Camera))} reference photo: shown next to its twin, not counted.");
        if (twins.TryGetValue(image.Id, out var twin))
            parts.Add($"Paired with {twin.FileName}.");
        parts.AddRange(warnings);
        if (counted && image.LocationFromTwin)
            parts.Add($"GPS copied from its {ImagePairing.Label(ImagePairing.Other(image.Camera))} twin.");
        else if (counted && image.Latitude is null)
            parts.Add("No GPS location in the photo; it won't appear on the map.");
        return parts.Count > 0 ? string.Join(" ", parts) : null;
    }

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private async Task<OneFile> UploadOneAsync(Survey survey, UploadedFile file, HashSet<string> knownHashes,
        List<string> writtenKeys, DateTimeOffset now, CancellationToken ct)
    {
        var name = CleanFileName(file.FileName);
        OneFile Reject(string message) => new(new UploadFileResult(name, UploadFileStatus.Rejected, message, null), null, Array.Empty<string>());

        var extension = Path.GetExtension(name);
        if (!SurveyImageRules.Extensions.TryGetValue(extension, out var expectedType))
            return Reject("Only JPEG (.jpg, .jpeg) and PNG (.png) photos are accepted.");
        if (file.Length <= 0)
            return Reject("The file is empty.");
        if (file.Length > SurveyImageRules.MaxFileBytes)
            return Reject($"The file is larger than {SurveyImageRules.MaxFileBytes / (1024 * 1024)} MB.");
        if (!file.Content.CanSeek)
            return Reject("The upload could not be read; please try again.");

        // Real content type from the first bytes, so a renamed .exe or script is refused.
        var header = new byte[8];
        file.Content.Position = 0;
        var read = await file.Content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        var contentType = SurveyImageRules.SniffContentType(header.AsSpan(0, read));
        if (contentType is null)
            return Reject("The file is not a JPEG or PNG image.");
        if (contentType != expectedType)
            return Reject($"The file's content ({contentType}) doesn't match its extension ({extension}).");

        file.Content.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(file.Content, ct)).ToLowerInvariant();
        if (!knownHashes.Add(hash))
            return new(new UploadFileResult(name, UploadFileStatus.Duplicate, "This photo is already in the survey.", null), null, Array.Empty<string>());

        var (namedCamera, pairKey) = ImagePairing.Classify(name);
        var image = new SurveyImage
        {
            SurveyId = survey.Id,
            FileName = name,
            Camera = namedCamera ?? survey.Camera, // final camera is set when twins are matched (PairAsync)
            PairKey = pairKey,
            ContentType = contentType,
            Sha256 = hash,
            CreatedAt = now,
            UpdatedAt = now,
        };
        image.StoragePath = SurveyImageStorageKeys.Original(survey.Id, image.Id, extension);

        file.Content.Position = 0;
        image.SizeBytes = await _storage.SaveAsync(image.StoragePath, file.Content, ct);
        writtenKeys.Add(image.StoragePath);

        // Read back from storage (not the upload stream), so what we checked is exactly what we kept.
        ImageMetadata? metadata;
        await using (var stored = await OpenStoredAsync(image.StoragePath, ct))
            metadata = _processor.ReadMetadata(stored);

        if (metadata is null)
        {
            await TryDeleteAsync(image.StoragePath);
            writtenKeys.Remove(image.StoragePath);
            knownHashes.Remove(hash);
            return Reject("The file couldn't be read as an image (it may be damaged).");
        }

        image.WidthPx = metadata.WidthPx;
        image.HeightPx = metadata.HeightPx;
        image.Latitude = metadata.Latitude;
        image.Longitude = metadata.Longitude;
        if (metadata.TakenLocal is { } local)
            image.TakenAt = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                metadata.TakenUtcOffset ?? SurveyImageRules.DefaultUtcOffset);

        var warnings = new List<string>();
        byte[]? thumbnail;
        await using (var stored = await OpenStoredAsync(image.StoragePath, ct))
            thumbnail = _processor.CreateJpegThumbnail(stored, SurveyImageRules.ThumbnailMaxSize);
        if (thumbnail is not null)
        {
            var thumbKey = SurveyImageStorageKeys.Thumbnail(survey.Id, image.Id);
            using var thumbStream = new MemoryStream(thumbnail, writable: false);
            await _storage.SaveAsync(thumbKey, thumbStream, ct);
            writtenKeys.Add(thumbKey);
            image.ThumbnailPath = thumbKey;
        }
        else
        {
            warnings.Add("No preview could be made; the full photo will be shown instead.");
        }

        if (image.TakenAt is null) warnings.Add("No capture time in the photo; the survey's date and time are used.");
        // GPS and pairing notes are added after twins are matched (Message).

        return new(new UploadFileResult(name, UploadFileStatus.Uploaded, null, null), image, warnings);
    }

    private async Task<Stream> OpenStoredAsync(string key, CancellationToken ct) =>
        await _storage.OpenReadAsync(key, ct) ?? throw new InvalidOperationException($"Stored file '{key}' is missing.");

    private async Task TryDeleteAsync(string key)
    {
        try { await _storage.DeleteAsync(key); }
        catch { /* best effort: an orphan file is harmless, a failed request is not */ }
    }

    /// <summary>Just the file name (browsers may send a path), trimmed to the column length.</summary>
    private static string CleanFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/')).Trim();
        if (name.Length == 0) name = "photo";
        if (name.Length > SurveyImage.FileNameMaxLength)
        {
            var ext = Path.GetExtension(name);
            name = name[..(SurveyImage.FileNameMaxLength - ext.Length)] + ext;
        }
        return name;
    }

    private static SurveyImageDto ToDto(Survey survey, SurveyImage i, IReadOnlyDictionary<Guid, SurveyImage> twins) => new(
        i.Id, i.SurveyId, i.FileName, i.ContentType, i.SizeBytes, i.WidthPx, i.HeightPx, i.TakenAt, i.Latitude, i.Longitude,
        i.ThumbnailPath is not null, i.Reviewed, i.CreatedAt,
        i.Camera, !ImagePairing.IsCounted(survey, i), twins.TryGetValue(i.Id, out var twin) ? twin.Id : null, i.LocationFromTwin);
}
