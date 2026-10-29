using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SkiaSharp;
using AerialWatch.Application.Common;

namespace AerialWatch.Infrastructure.Imaging;

/// <summary>
/// IImageProcessor with SkiaSharp (MIT; decode + thumbnails) and MetadataExtractor (Apache-2.0; EXIF time and GPS).
/// Never throws for a bad image: returns null so the upload can reject that one file.
/// </summary>
public sealed class SkiaImageProcessor : IImageProcessor
{
    private const int TagOffsetTimeOriginal = 0x9011; // EXIF 2.31 "OffsetTimeOriginal", e.g. "+09:30"

    public ImageMetadata? ReadMetadata(Stream image)
    {
        int width, height;
        try
        {
            image.Position = 0;
            using var skStream = new SKManagedStream(image, disposeManagedStream: false);
            using var codec = SKCodec.Create(skStream);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0) return null;
            width = codec.Info.Width;
            height = codec.Info.Height;
        }
        catch (Exception)
        {
            return null;
        }

        DateTime? taken = null;
        TimeSpan? offset = null;
        double? latitude = null, longitude = null;
        try
        {
            image.Position = 0;
            var directories = ImageMetadataReader.ReadMetadata(image);

            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd is not null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var original))
            {
                taken = original;
                offset = ParseOffset(subIfd.GetString(TagOffsetTimeOriginal));
            }
            else if (directories.OfType<ExifIfd0Directory>().FirstOrDefault() is { } ifd0
                     && ifd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var modified))
            {
                taken = modified;
            }

            if (directories.OfType<GpsDirectory>().FirstOrDefault()?.GetGeoLocation() is { } geo && !geo.IsZero
                && Math.Abs(geo.Latitude) <= 90 && Math.Abs(geo.Longitude) <= 180)
            {
                latitude = Math.Round(geo.Latitude, 7);
                longitude = Math.Round(geo.Longitude, 7);
            }
        }
        catch (Exception)
        {
            // Missing or damaged EXIF is fine: the photo is still usable without time/GPS.
        }

        return new ImageMetadata(width, height, taken, offset, latitude, longitude);
    }

    public byte[]? CreateJpegThumbnail(Stream image, int maxSize)
    {
        try
        {
            image.Position = 0;
            using var skStream = new SKManagedStream(image, disposeManagedStream: false);
            using var codec = SKCodec.Create(skStream);
            if (codec is null) return null;

            var info = codec.Info;
            var scale = Math.Min(1f, (float)maxSize / Math.Max(info.Width, info.Height));
            var targetWidth = Math.Max(1, (int)Math.Round(info.Width * scale));
            var targetHeight = Math.Max(1, (int)Math.Round(info.Height * scale));

            // JPEG can decode straight at 1/2, 1/4 or 1/8 size, which is much faster for 20+ MP drone photos.
            var decodeSize = codec.GetScaledDimensions(scale);
            var decodeInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var decoded = new SKBitmap(decodeInfo);
            var result = codec.GetPixels(decodeInfo, decoded.GetPixels());
            if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput)) return null;

            using var resized = decoded.Width == targetWidth && decoded.Height == targetHeight
                ? decoded.Copy()
                : decoded.Resize(new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.Medium);
            if (resized is null) return null;

            using var img = SKImage.FromBitmap(resized);
            using var data = img.Encode(SKEncodedImageFormat.Jpeg, 80);
            return data?.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static TimeSpan? ParseOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        var negative = text.StartsWith('-');
        if (text[0] is '+' or '-') text = text[1..];
        if (!TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var span)) return null;
        return negative ? -span : span;
    }
}
