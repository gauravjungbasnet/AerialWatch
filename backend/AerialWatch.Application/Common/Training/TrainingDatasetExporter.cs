using System.Text;
using System.Text.Json;
using AerialWatch.Contracts.Common;
using AerialWatch.Contracts.Common.Training;
using AerialWatch.Domain.Common.Training;

namespace AerialWatch.Application.Common.Training;

/// <summary>
/// Shared YOLO dataset export for every module (D-038). Applies the dataset rules that don't depend on the module:
///  R6 every split needs at least one photo · R7 new folder, never overwritten · R8 photos without a size or with a
///  missing file are skipped with a warning · R4 photos without objects get an empty label file.
/// Selecting which photos/boxes are usable (R2, R3, R5) is the module's job before it calls this.
/// </summary>
public sealed class TrainingDatasetExporter
{
    private static readonly TrainingSplit[] Splits = { TrainingSplit.Train, TrainingSplit.Validation, TrainingSplit.Test };
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ITrainingDatasetWriter _writer;
    private readonly IClock _clock;

    public TrainingDatasetExporter(ITrainingDatasetWriter writer, IClock clock)
    {
        _writer = writer;
        _clock = clock;
    }

    public string ExportRoot => _writer.ExportRoot;

    /// <summary>Checks rule R6 without writing anything; returns the split names that have no usable photo.</summary>
    public static IReadOnlyList<TrainingSplit> MissingSplits(IEnumerable<TrainingDatasetItem> items)
    {
        var present = items.Where(i => i.WidthPx > 0 && i.HeightPx > 0).Select(i => i.Split).ToHashSet();
        return Splits.Where(s => !present.Contains(s)).ToList();
    }

    public async Task<TrainingExportResultDto> ExportAsync(TrainingExportSpec spec, CancellationToken ct = default)
    {
        var missing = MissingSplits(spec.Items);
        if (missing.Count > 0)
            throw new RequestValidationException("split",
                $"Every set needs at least one checked photo. Missing: {string.Join(", ", missing)}.");

        var createdAt = _clock.UtcNow;
        var warnings = new List<string>();
        var written = new List<(TrainingDatasetItem Item, int Labels)>();
        var handle = await _writer.BeginAsync(spec.DatasetName, ct);

        foreach (var item in spec.Items)
        {
            if (item.WidthPx is not > 0 || item.HeightPx is not > 0)
            {
                warnings.Add($"Skipped {item.FileName} ({item.SessionName}): image size unknown.");
                continue;
            }

            var folder = YoloLabels.FolderOf(item.Split);
            var stem = $"{Slug(item.SessionName)}_{Path.GetFileNameWithoutExtension(item.StorageKey)}";
            var ext = Path.GetExtension(item.StorageKey).ToLowerInvariant();
            if (ext.Length == 0) ext = ".jpg";

            if (!await _writer.CopyImageAsync(handle, item.StorageKey, $"images/{folder}/{stem}{ext}", ct))
            {
                warnings.Add($"Skipped {item.FileName} ({item.SessionName}): stored file is missing.");
                continue;
            }

            var labelText = YoloLabels.File(item.Labels, item.WidthPx.Value, item.HeightPx.Value);
            await _writer.WriteTextAsync(handle, $"labels/{folder}/{stem}.txt", labelText, ct);
            written.Add((item, labelText.Length == 0 ? 0 : labelText.Count(c => c == '\n')));
        }

        var totals = Splits.Select(s =>
        {
            var part = written.Where(w => w.Item.Split == s).ToList();
            return new TrainingSplitTotalsDto(s,
                part.Select(w => w.Item.SessionName).Distinct().Count(),
                part.Count,
                part.Count(w => w.Labels > 0),
                part.Sum(w => w.Labels));
        }).ToList();
        warnings.AddRange(totals.Where(t => t.Photos == 0).Select(t => $"The {t.Split} set ended up empty (all its files were skipped)."));

        await _writer.WriteTextAsync(handle, "data.yaml", YoloLabels.DataYaml(spec.ClassNames), ct);
        await _writer.WriteTextAsync(handle, "dataset-card.json", Card(spec, createdAt, totals, warnings), ct);
        var path = await _writer.CompleteAsync(handle, ct);

        return new TrainingExportResultDto(spec.DatasetName, path, totals, warnings, createdAt);
    }

    private static string Card(TrainingExportSpec spec, DateTimeOffset createdAt, IReadOnlyList<TrainingSplitTotalsDto> totals, IReadOnlyList<string> warnings)
    {
        var card = new Dictionary<string, object?>
        {
            ["dataset"] = spec.DatasetName,
            ["createdAt"] = createdAt,
            ["createdBy"] = spec.CreatedBy,
            ["format"] = "YOLO detection (class xc yc w h, relative)",
            ["classes"] = spec.ClassNames,
            ["note"] = "Local only. May contain restricted data (docs/11). Do not commit or upload.",
            ["sessions"] = spec.Items.Select(i => new { name = i.SessionName, split = i.Split.ToString() }).Distinct().ToList(),
            ["totals"] = totals.Select(t => new { split = t.Split.ToString(), t.Sessions, t.Photos, t.PhotosWithLabels, t.Labels }).ToList(),
            ["warnings"] = warnings,
        };
        foreach (var (key, value) in spec.CardExtras) card[key] = value;
        return JsonSerializer.Serialize(card, Json);
    }

    /// <summary>File-name-safe version of a session name: letters, digits, '-' and '_' only, at most 40 characters.</summary>
    public static string Slug(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.Trim()) sb.Append(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_');
        var slug = sb.ToString().Trim('_');
        if (slug.Length == 0) slug = "session";
        return slug.Length > 40 ? slug[..40] : slug;
    }
}
