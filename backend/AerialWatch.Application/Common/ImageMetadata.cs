namespace AerialWatch.Application.Common;

/// <summary>
/// What could be read from an image: pixel size (always, if the image decodes) and EXIF capture time and GPS
/// (when present). TakenLocal is the camera's clock; TakenUtcOffset is set only if the file records its time zone.
/// </summary>
public sealed record ImageMetadata(
    int WidthPx,
    int HeightPx,
    DateTime? TakenLocal,
    TimeSpan? TakenUtcOffset,
    double? Latitude,
    double? Longitude);
