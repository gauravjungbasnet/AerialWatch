namespace AerialWatch.Core.Entities;

/// <summary>A bounding box around an animal in an image, from the AI or drawn by a ranger.</summary>
public class AnimalDetection : EntityBase
{
    public Guid ImageId { get; set; }
    public SurveyImage? Image { get; set; }

    /// <summary>Box in original image pixels.</summary>
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public string Label { get; set; } = "pig";

    /// <summary>0–1 from the AI; null when added by a ranger.</summary>
    public double? Confidence { get; set; }

    public DetectionSource Source { get; set; } = DetectionSource.Ai;
    public DetectionStatus Status { get; set; } = DetectionStatus.Pending;

    /// <summary>AI model that produced it (traceability).</summary>
    public string? ModelVersion { get; set; }
}
