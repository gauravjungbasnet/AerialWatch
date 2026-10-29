using AerialWatch.Domain.Common;
using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Domain.Wfp.Surveys;

/// <summary>
/// A drone photo belonging to a survey. The file itself lives in file storage (D-030); this row holds its metadata
/// and the storage keys. CreatedAt is the upload time.
/// </summary>
public class SurveyImage : EntityBase
{
    public const int FileNameMaxLength = 260;
    public const int StoragePathMaxLength = 500;
    public const int ContentTypeMaxLength = 50;
    public const int Sha256Length = 64;
    public const int PairKeyMaxLength = 260;

    public Guid SurveyId { get; set; }
    public Survey? Survey { get; set; }

    /// <summary>Name the file had on the uploader's computer (display only; never used as a path).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Storage key of the original, e.g. wfp/surveys/{surveyId}/originals/{imageId}.jpg.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Storage key of the ~400 px JPEG thumbnail; null if one couldn't be made.</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>image/jpeg or image/png (from the file's content, not its extension).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>SHA-256 of the file (lower-case hex). Unique per survey, so the same photo isn't counted twice.</summary>
    public string Sha256 { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }

    /// <summary>From EXIF, when present.</summary>
    public DateTimeOffset? TakenAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>The ranger has checked every box on this photo (D-034).</summary>
    public bool Reviewed { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>"Needs a second look": set by a ranger who isn't sure; doesn't block completing the review.</summary>
    public bool Flagged { get; set; }

    /// <summary>
    /// Camera that took this photo (D-039). Photos from the survey's own camera are counted and reviewed; photos from the
    /// other camera are reference twins, shown next to their pair but never counted, sent to the AI or used for training.
    /// </summary>
    public CameraType Camera { get; set; }

    /// <summary>
    /// File name without extension and camera suffix, lower case (e.g. "ix-12-37642_0032_0023" for both
    /// "IX-12-37642_0032_0023_THM.JPG" and "IX-12-37642_0032_0023.JPG"). Photos with the same key are twins.
    /// </summary>
    public string? PairKey { get; set; }

    /// <summary>True when Latitude/Longitude were copied from the twin because this photo had no GPS of its own.</summary>
    public bool LocationFromTwin { get; set; }

    public List<AnimalDetection> Detections { get; set; } = new();
}
