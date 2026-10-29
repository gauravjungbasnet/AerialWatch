using AerialWatch.Application.Common;

namespace AerialWatch.UnitTests.TestDoubles;

/// <summary>In-memory IFileStorage: key → bytes.</summary>
internal sealed class FakeFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new();
    public List<string> DeletedFolders { get; } = new();

    /// <summary>Set to make the next SaveAsync throw (simulates a full disk).</summary>
    public bool FailNextSave { get; set; }

    public async Task<long> SaveAsync(string key, Stream content, CancellationToken ct = default)
    {
        if (FailNextSave) { FailNextSave = false; throw new IOException("Disk full"); }
        if (Files.ContainsKey(key)) throw new IOException($"Exists: {key}");
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, ct);
        Files[key] = copy.ToArray();
        return copy.Length;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default) =>
        Task.FromResult<Stream?>(Files.TryGetValue(key, out var bytes) ? new MemoryStream(bytes, writable: false) : null);

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        Files.Remove(key);
        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(string folderKey, CancellationToken ct = default)
    {
        DeletedFolders.Add(folderKey);
        foreach (var key in Files.Keys.Where(k => k.StartsWith(folderKey + "/", StringComparison.Ordinal)).ToList()) Files.Remove(key);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Pretends to decode images: any JPEG/PNG bytes decode unless they contain "CORRUPT"; "NOTHUMB" makes thumbnails fail.
/// Metadata returned is whatever the test sets.
/// </summary>
internal sealed class FakeImageProcessor : IImageProcessor
{
    public ImageMetadata Metadata { get; set; } = new(4000, 3000, new DateTime(2026, 2, 10, 18, 21, 5), null, -14.4652, 132.2635);

    public ImageMetadata? ReadMetadata(Stream image) => Text(image).Contains("CORRUPT") ? null : Metadata;

    public byte[]? CreateJpegThumbnail(Stream image, int maxSize) =>
        Text(image) is var t && (t.Contains("CORRUPT") || t.Contains("NOTHUMB")) ? null : new byte[] { 0xFF, 0xD8, 0xFF, 1, 2, 3 };

    private static string Text(Stream s)
    {
        s.Position = 0;
        using var reader = new StreamReader(s, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
