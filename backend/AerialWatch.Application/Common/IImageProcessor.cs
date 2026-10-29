namespace AerialWatch.Application.Common;

/// <summary>Reads image metadata and makes thumbnails. Implemented in Infrastructure/Imaging.</summary>
public interface IImageProcessor
{
    /// <summary>Size and EXIF of the image, or null if it can't be decoded as an image.</summary>
    ImageMetadata? ReadMetadata(Stream image);

    /// <summary>A JPEG no wider/taller than maxSize pixels, or null if the image can't be decoded.</summary>
    byte[]? CreateJpegThumbnail(Stream image, int maxSize);
}
