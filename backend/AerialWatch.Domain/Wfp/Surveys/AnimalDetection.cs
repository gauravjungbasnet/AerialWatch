using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.Surveys;

/// <summary>A bounding box around an animal in an image, from the AI or drawn by a ranger.</summary>
public class AnimalDetection : EntityBase
{
    public const int LabelMaxLength = 40;
    public const int ModelVersionMaxLength = 60;
    public const int ReviewedByMaxLength = 100;

    /// <summary>Label given to ranger-drawn boxes and to the detector's pig class.</summary>
    public const string PigLabel = "pig";

    public Guid ImageId { get; set; }
    public SurveyImage? Image { get; set; }

    /// <summary>Box in original image pixels.</summary>
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public string Label { get; set; } = PigLabel;

    /// <summary>0–1 from the AI; null when added by a ranger.</summary>
    public double? Confidence { get; set; }

    public DetectionSource Source { get; set; } = DetectionSource.Ai;
    public DetectionStatus Status { get; set; } = DetectionStatus.Pending;

    /// <summary>AI model that produced it (traceability).</summary>
    public string? ModelVersion { get; set; }

    /// <summary>Who last accepted, rejected, drew or moved the box (D-034); null while an AI box is untouched.</summary>
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
