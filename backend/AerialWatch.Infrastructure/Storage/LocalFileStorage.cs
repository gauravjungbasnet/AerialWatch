using System.Text.RegularExpressions;
using AerialWatch.Application.Common;

namespace AerialWatch.Infrastructure.Storage;

/// <summary>
/// IFileStorage on the local disk under one root folder (D-030). Keys are relative forward-slash paths; any key that is
/// absolute, contains "." / ".." segments or unusual characters is refused, so a key can never reach outside the root.
/// Writes go to a .tmp file first and are renamed when complete, so a crash never leaves a half-written file under the
/// real name.
/// </summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;
    private readonly string _root;

    public LocalFileStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Storage root path is required.", nameof(rootPath));
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
    }

    public string RootPath => _root;

    public async Task<long> SaveAsync(string key, Stream content, CancellationToken ct = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path)) throw new IOException($"A file already exists at '{key}'.");

        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            long written;
            await using (var target = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                await content.CopyToAsync(target, BufferSize, ct);
                written = target.Length;
            }
            File.Move(temp, path, overwrite: false);
            return written;
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(string folderKey, CancellationToken ct = default)
    {
        var path = Resolve(folderKey);
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        return Task.CompletedTask;
    }

    /// <summary>Physical path for a key, refusing anything that could escape the root.</summary>
    public string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !SafeKey().IsMatch(key))
            throw new ArgumentException($"Invalid storage key '{key}'.", nameof(key));
        if (key.Split('/').Any(segment => segment is "" or "." or ".."))
            throw new ArgumentException($"Invalid storage key '{key}'.", nameof(key));

        var full = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Storage key '{key}' points outside the storage root.", nameof(key));
        return full;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // Relative path of letters, digits, '.', '_', '-' and '/' only (no drive letters, backslashes or spaces).
    [GeneratedRegex(@"^[A-Za-z0-9._\-]+(/[A-Za-z0-9._\-]+)*$")]
    private static partial Regex SafeKey();
}
