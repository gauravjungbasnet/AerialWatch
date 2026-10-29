using System.Globalization;
using System.Text;
using AerialWatch.Domain.Common.Training;

namespace AerialWatch.Application.Common.Training;

/// <summary>YOLO detection format (D-038): one line per object, "class xc yc w h", all relative to the photo size.</summary>
public static class YoloLabels
{
    /// <summary>The label line, with the box clamped to the photo; null if less than 1 px is left after clamping.</summary>
    public static string? Line(TrainingLabel label, int width, int height)
    {
        if (width <= 0 || height <= 0) return null;
        var x0 = Math.Clamp(label.X, 0, width);
        var y0 = Math.Clamp(label.Y, 0, height);
        var x1 = Math.Clamp(label.X + label.Width, 0, width);
        var y1 = Math.Clamp(label.Y + label.Height, 0, height);
        if (x1 - x0 < 1 || y1 - y0 < 1) return null;

        return string.Create(CultureInfo.InvariantCulture,
            $"{label.ClassId} {(x0 + x1) / 2 / width:0.000000} {(y0 + y1) / 2 / height:0.000000} {(x1 - x0) / width:0.000000} {(y1 - y0) / height:0.000000}");
    }

    /// <summary>All lines for one photo, newline-terminated; empty text for a photo with no objects (a "nothing here" example).</summary>
    public static string File(IEnumerable<TrainingLabel> labels, int width, int height)
    {
        var lines = labels.Select(l => Line(l, width, height)).Where(l => l is not null).ToList();
        return lines.Count == 0 ? string.Empty : string.Join('\n', lines) + "\n";
    }

    /// <summary>data.yaml without a "path:" line, so YOLO resolves the folders next to the file and the dataset can be moved.</summary>
    public static string DataYaml(IReadOnlyList<string> classNames)
    {
        var sb = new StringBuilder("train: images/train\nval: images/val\ntest: images/test\nnames:\n");
        for (var i = 0; i < classNames.Count; i++) sb.Append(CultureInfo.InvariantCulture, $"  {i}: {classNames[i]}\n");
        return sb.ToString();
    }

    /// <summary>YOLO folder name for a split.</summary>
    public static string FolderOf(TrainingSplit split) => split switch
    {
        TrainingSplit.Train => "train",
        TrainingSplit.Validation => "val",
        TrainingSplit.Test => "test",
        _ => throw new ArgumentOutOfRangeException(nameof(split)),
    };
}
