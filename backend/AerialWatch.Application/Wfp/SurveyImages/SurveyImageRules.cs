using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.SurveyImages;

/// <summary>Upload rules (D-030). The frontend mirrors MaxFileBytes, MaxFilesPerRequest and the allowed types.</summary>
public static class SurveyImageRules
{
    public const long MaxFileBytes = 50L * 1024 * 1024;
    public const int MaxFilesPerRequest = 20;
    public const int ThumbnailMaxSize = 400;

    /// <summary>Largest request the API accepts on the upload endpoint (20 × 50 MB plus multipart overhead).</summary>
    public const long MaxRequestBytes = MaxFilesPerRequest * MaxFileBytes + 10L * 1024 * 1024;

    /// <summary>
    /// Time zone assumed for EXIF times that don't record one (most drones): Northern Territory, ACST, UTC+09:30,
    /// no daylight saving.
    /// </summary>
    public static readonly TimeSpan DefaultUtcOffset = new(9, 30, 0);

    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";

    /// <summary>Allowed extensions and the content type each must actually contain.</summary>
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = Jpeg,
        [".jpeg"] = Jpeg,
        [".png"] = Png,
    };

    /// <summary>Photos can be added or removed only before review starts.</summary>
    public static bool AcceptsChanges(SurveyStatus status) => status is SurveyStatus.Draft or SurveyStatus.Uploading;

    /// <summary>
    /// After review starts, only reference twins from the other camera can still be added (D-040): they are never
    /// counted, so they can't change the review or the estimate.
    /// </summary>
    public static bool AcceptsReferencePhotos(SurveyStatus status) =>
        status is SurveyStatus.Processing or SurveyStatus.InReview or SurveyStatus.Reviewed;

    /// <summary>Content type from the file's first bytes ("magic number"), or null if it isn't JPEG or PNG.</summary>
    public static string? SniffContentType(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return Jpeg;
        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return Png;
        return null;
    }
}
