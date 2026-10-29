namespace AerialWatch.Core.Entities;

/// <summary>A drone photo belonging to a survey. File bytes live in file storage; this row holds metadata.</summary>
public class SurveyImage : EntityBase
{
    public Guid SurveyId { get; set; }
    public Survey? Survey { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }

    /// <summary>From EXIF, when present.</summary>
    public DateTimeOffset? TakenAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool Reviewed { get; set; }

    public List<AnimalDetection> Detections { get; set; } = new();
}
