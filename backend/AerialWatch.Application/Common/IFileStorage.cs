namespace AerialWatch.Application.Common;

/// <summary>
/// Where uploaded files live (D-030). Keys are relative, forward-slash paths such as
/// "wfp/surveys/{surveyId}/originals/{imageId}.jpg"; the implementation decides the physical location
/// (local disk now, cloud storage later) and must refuse keys that escape its root.
/// </summary>
public interface IFileStorage
{
    /// <summary>Writes the stream to the key. Never overwrites an existing file. Returns the number of bytes written.</summary>
    Task<long> SaveAsync(string key, Stream content, CancellationToken ct = default);

    /// <summary>Opens the file for reading, or null if it doesn't exist. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default);

    /// <summary>Deletes one file; no error if it is already gone.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Deletes everything under a folder key, e.g. "wfp/surveys/{surveyId}"; no error if it doesn't exist.</summary>
    Task DeleteFolderAsync(string folderKey, CancellationToken ct = default);
}
