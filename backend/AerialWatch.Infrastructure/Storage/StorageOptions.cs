namespace AerialWatch.Infrastructure.Storage;

/// <summary>Configuration section "Storage" in AerialWatch.Api/appsettings.json (D-030).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Folder that holds uploaded files, outside the repository and the web root, e.g. C:\AerialWatchData.
    /// If not set, a folder under the system temp directory is used (fine for tests, not for real data).
    /// </summary>
    public string? RootPath { get; set; }
}
