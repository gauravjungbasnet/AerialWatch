using AerialWatch.Domain.Common;

namespace AerialWatch.Domain.Wfp.Surveys;

/// <summary>
/// A pig's position on the twin photo from the other camera (D-040, D-041). Two ways in:
/// <list type="bullet">
/// <item>From a confirmed pig on the counted photo (DetectionId set): checked on the twin as placed or not visible.</item>
/// <item>Drawn first on the twin (DetectionId null): a <em>twin sighting</em>. The app suggests where it is on the counted
/// photo; when the ranger confirms that, a counted detection is created and linked here. If the pig can't be seen on the
/// counted photo, NotVisibleOnCounted is set and it stays a sighting on the other camera only.</item>
/// </list>
/// Never counted itself: the count is always the detections on the survey's own camera. Confirmed marks with a detection
/// are matching points between the two cameras, so they also improve the suggestions both ways.
/// </summary>
public class TwinMark : EntityBase
{
    /// <summary>The counted pig; null for a twin sighting not (yet) confirmed on the counted photo.</summary>
    public Guid? DetectionId { get; set; }
    public AnimalDetection? Detection { get; set; }

    /// <summary>The counted photo this mark belongs to (the survey's own camera).</summary>
    public Guid CountedImageId { get; set; }

    /// <summary>The twin photo the mark is on (no foreign key: the photo row is deleted with its survey anyway).</summary>
    public Guid TwinImageId { get; set; }

    public TwinMarkStatus Status { get; set; }

    /// <summary>Box on the twin in its original pixels; null when NotVisible.</summary>
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }

    /// <summary>A twin sighting the ranger can't see on the counted photo (e.g. seen in colour, not in thermal).</summary>
    public bool NotVisibleOnCounted { get; set; }

    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
