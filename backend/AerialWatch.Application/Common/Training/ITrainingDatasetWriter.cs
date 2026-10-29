namespace AerialWatch.Application.Common.Training;

/// <summary>
/// Writes a dataset folder under the export root (local disk; Infrastructure). Work happens in "&lt;name&gt;.partial" and is
/// renamed when complete, so a half-written export never looks finished. Never overwrites an existing dataset.
/// </summary>
public interface ITrainingDatasetWriter
{
    /// <summary>Folder all datasets go under, e.g. C:\AerialWatchData\training-exports (shown in the UI).</summary>
    string ExportRoot { get; }

    /// <summary>Starts a dataset; returns a handle for the other calls. Throws if the dataset already exists.</summary>
    Task<string> BeginAsync(string datasetName, CancellationToken ct = default);

    /// <summary>Copies a stored photo into the dataset. False if the stored file is missing.</summary>
    Task<bool> CopyImageAsync(string handle, string storageKey, string relativePath, CancellationToken ct = default);

    Task WriteTextAsync(string handle, string relativePath, string text, CancellationToken ct = default);

    /// <summary>Finishes the dataset and returns its full folder path.</summary>
    Task<string> CompleteAsync(string handle, CancellationToken ct = default);
}
