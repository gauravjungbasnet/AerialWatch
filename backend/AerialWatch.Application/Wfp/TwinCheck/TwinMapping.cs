namespace AerialWatch.Application.Wfp.TwinCheck;

/// <summary>A box in original image pixels.</summary>
public readonly record struct PixelBox(double X, double Y, double Width, double Height)
{
    public double CentreX => X + Width / 2;
    public double CentreY => Y + Height / 2;
}

/// <summary>A pig's box on the counted photo and where the ranger placed it on the twin.</summary>
public readonly record struct MatchedBoxes(PixelBox Source, PixelBox Twin);

/// <summary>
/// Maps a box from the counted photo to its twin (D-040). Each axis is fitted separately: twin = scale × source + offset
/// (least squares on box centres), which covers the two cameras' different resolution, field of view and side-by-side
/// offset when flying level over flat ground. With no matching points it scales by the photo sizes (same relative
/// position); with one it keeps the size scale and takes the offset from that point. Lens distortion, hills and tree
/// height are not modelled, so positions are approximate and the ranger confirms or moves them.
/// </summary>
public sealed class TwinMapping
{
    private const double MinSpreadPx = 20; // points closer than this on an axis can't fix a scale

    public double ScaleX { get; }
    public double OffsetX { get; }
    public double ScaleY { get; }
    public double OffsetY { get; }
    public int LearnedFrom { get; }

    private TwinMapping(double sx, double ox, double sy, double oy, int learnedFrom)
    {
        ScaleX = sx; OffsetX = ox; ScaleY = sy; OffsetY = oy; LearnedFrom = learnedFrom;
    }

    /// <param name="defaultScaleX">Twin width ÷ source width (used when the points can't fix a scale).</param>
    public static TwinMapping Fit(IReadOnlyList<MatchedBoxes> points, double defaultScaleX, double defaultScaleY)
    {
        var (sx, ox) = FitAxis(points.Select(p => (p.Source.CentreX, p.Twin.CentreX)).ToList(), defaultScaleX);
        var (sy, oy) = FitAxis(points.Select(p => (p.Source.CentreY, p.Twin.CentreY)).ToList(), defaultScaleY);
        return new TwinMapping(sx, ox, sy, oy, points.Count);
    }

    private static (double Scale, double Offset) FitAxis(IReadOnlyList<(double S, double T)> pts, double defaultScale)
    {
        if (pts.Count == 0) return (defaultScale, 0);

        var meanS = pts.Average(p => p.S);
        var meanT = pts.Average(p => p.T);
        var varS = pts.Sum(p => (p.S - meanS) * (p.S - meanS));
        if (pts.Count >= 2 && pts.Max(p => p.S) - pts.Min(p => p.S) >= MinSpreadPx)
        {
            var scale = pts.Sum(p => (p.S - meanS) * (p.T - meanT)) / varS;
            // A wild scale means a mistaken mark; fall back rather than send every suggestion off the photo.
            if (scale > defaultScale / 5 && scale < defaultScale * 5) return (scale, meanT - scale * meanS);
        }
        return (defaultScale, meanT - defaultScale * meanS);
    }

    /// <summary>The reverse direction: a twin box on the counted photo, kept inside it (D-041).</summary>
    public PixelBox MapBack(PixelBox twin, double sourceWidth, double sourceHeight)
    {
        var w = Math.Clamp(twin.Width / ScaleX, 4, sourceWidth);
        var h = Math.Clamp(twin.Height / ScaleY, 4, sourceHeight);
        var x = Math.Clamp((twin.CentreX - OffsetX) / ScaleX - w / 2, 0, sourceWidth - w);
        var y = Math.Clamp((twin.CentreY - OffsetY) / ScaleY - h / 2, 0, sourceHeight - h);
        return new PixelBox(Math.Round(x, 1), Math.Round(y, 1), Math.Round(w, 1), Math.Round(h, 1));
    }

    /// <summary>The source box on the twin, kept inside the twin photo.</summary>
    public PixelBox Map(PixelBox source, double twinWidth, double twinHeight)
    {
        var w = Math.Clamp(source.Width * ScaleX, 4, twinWidth);
        var h = Math.Clamp(source.Height * ScaleY, 4, twinHeight);
        var x = Math.Clamp(ScaleX * source.CentreX + OffsetX - w / 2, 0, twinWidth - w);
        var y = Math.Clamp(ScaleY * source.CentreY + OffsetY - h / 2, 0, twinHeight - h);
        return new PixelBox(Math.Round(x, 1), Math.Round(y, 1), Math.Round(w, 1), Math.Round(h, 1));
    }
}
