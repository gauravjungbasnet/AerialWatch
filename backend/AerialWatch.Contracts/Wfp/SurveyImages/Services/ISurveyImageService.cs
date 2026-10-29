using AerialWatch.Contracts.Wfp.SurveyImages.Models;

namespace AerialWatch.Contracts.Wfp.SurveyImages.Services;

/// <summary>Photos of a survey: upload (with EXIF and thumbnails), list, download, delete. Rules in docs/06 (D-030).</summary>
public interface ISurveyImageService
{
    /// <summary>Adds up to 20 photos to a Draft/Uploading survey. Returns a result per file.</summary>
    Task<UploadImagesResponse> UploadAsync(Guid surveyId, IReadOnlyList<UploadedFile> files, CancellationToken ct = default);

    /// <summary>Photos of a survey, by time taken (then file name).</summary>
    Task<IReadOnlyList<SurveyImageDto>> ListAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>The original photo, or its thumbnail (falls back to the original if there is no thumbnail).</summary>
    Task<ImageFile> OpenAsync(Guid imageId, bool thumbnail, CancellationToken ct = default);

    /// <summary>Removes a photo (row and files) while the survey is still Draft/Uploading.</summary>
    Task DeleteAsync(Guid imageId, CancellationToken ct = default);
}
