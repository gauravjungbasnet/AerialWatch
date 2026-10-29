using System.Text;
using System.Text.RegularExpressions;
using AerialWatch.Application.Common;
using AerialWatch.Application.Common.Training;

namespace AerialWatch.Infrastructure.Common.Training;

/// <summary>
/// Writes training datasets to local disk under one export root (D-038), shared by all modules.
/// A dataset is built in "&lt;name&gt;.partial" and renamed when complete; an existing dataset is never overwritten.
/// Dataset names and relative paths are checked so nothing can be written outside the export root.
/// </summary>
public sealed partial class LocalTrainingDatasetWriter : ITrainingDatasetWriter
{
    private const string PartialSuffix = ".partial";
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IFileStorage _storage;
    private readonly string _root;

    public LocalTrainingDatasetWriter(IFileStorage storage, string exportRoot)
    {
        if (string.IsNullOrWhiteSpace(exportRoot)) throw new ArgumentException("Export root is required.", nameof(exportRoot));
        _storage = storage;
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(exportRoot));
    }

    public string ExportRoot => _root;

    public Task<string> BeginAsync(string datasetName, CancellationToken ct = default)
    {
        if (!SafeName().IsMatch(datasetName))
            throw new ArgumentException($"Invalid dataset name '{datasetName}'.", nameof(datasetName));

        var finalPath = Path.Combine(_root, datasetName.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(finalPath)) throw new IOException($"Dataset '{datasetName}' already exists.");

        var partialPath = finalPath + PartialSuffix;
        if (Directory.Exists(partialPath)) Directory.Delete(partialPath, recursive: true); // leftover from an interrupted export
        Directory.CreateDirectory(partialPath);
        return Task.FromResult(partialPath);
    }

    public async Task<bool> CopyImageAsync(string handle, string storageKey, string relativePath, CancellationToken ct = default)
    {
        var target = Resolve(handle, relativePath);
        await using var source = await _storage.OpenReadAsync(storageKey, ct);
        if (source is null) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(output, ct);
        return true;
    }

    public async Task WriteTextAsync(string handle, string relativePath, string text, CancellationToken ct = default)
    {
        var target = Resolve(handle, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllTextAsync(target, text, Utf8NoBom, ct);
    }

    public Task<string> CompleteAsync(string handle, CancellationToken ct = default)
    {
        var partialPath = CheckHandle(handle);
        var finalPath = partialPath[..^PartialSuffix.Length];
        Directory.Move(partialPath, finalPath);
        return Task.FromResult(finalPath);
    }

    private string CheckHandle(string handle)
    {
        var full = Path.GetFullPath(handle);
        if (!full.EndsWith(PartialSuffix, StringComparison.Ordinal)
            || !full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(full))
            throw new ArgumentException("Unknown dataset handle.", nameof(handle));
        return full;
    }

    private string Resolve(string handle, string relativePath)
    {
        var folder = CheckHandle(handle);
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath)
            || relativePath.Split('/', '\\').Any(part => part is "" or "." or ".."))
            throw new ArgumentException($"Invalid dataset path '{relativePath}'.", nameof(relativePath));

        var full = Path.GetFullPath(Path.Combine(folder, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Dataset path '{relativePath}' points outside the dataset.", nameof(relativePath));
        return full;
    }

    // e.g. "wfp/thermal-20261027-101500": lower-case letters, digits and '-', at most one '/'.
    [GeneratedRegex(@"^[a-z0-9-]+(/[a-z0-9-]+)?$")]
    private static partial Regex SafeName();
}
