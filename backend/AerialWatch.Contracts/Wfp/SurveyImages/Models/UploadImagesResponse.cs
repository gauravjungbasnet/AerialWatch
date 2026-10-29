namespace AerialWatch.Contracts.Wfp.SurveyImages.Models;

/// <summary>Per-file results of one upload batch, plus totals. One bad file never fails the rest.</summary>
public sealed record UploadImagesResponse(
    Guid SurveyId,
    int Uploaded,
    int Duplicates,
    int Rejected,
    int TotalImages,
    IReadOnlyList<UploadFileResult> Files);
