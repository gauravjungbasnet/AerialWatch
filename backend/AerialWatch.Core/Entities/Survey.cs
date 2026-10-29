using AerialWatch.Core.Detection;

namespace AerialWatch.Core.Entities;

/// <summary>One drone flight: its conditions, photos, detections and (later) corrected estimate.</summary>
public class Survey : EntityBase
{
    public const int NameMaxLength = 80;
    public const int SiteNameMaxLength = 120;
    public const int NotesMaxLength = 1000;

    public string Name { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public DateOnly SurveyDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public Season Season { get; set; }
    public CameraType Camera { get; set; }

    /// <summary>Nearest study window for StartTime; null when outside the study's windows (night).</summary>
    public TimeWindow? TimeWindow { get; set; }

    public double? AltitudeM { get; set; }
    public double? AreaKm2 { get; set; }
    public GroundCover GroundCover { get; set; } = GroundCover.Unknown;
    public string? Notes { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    public List<SurveyImage> Images { get; set; } = new();
}
