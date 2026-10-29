namespace AerialWatch.Contracts.Wfp.SurveyImages.Models;

/// <summary>Result for one file. Image is set when Status is Uploaded; Message explains Duplicate/Rejected or gives a warning.</summary>
public sealed record UploadFileResult(string FileName, UploadFileStatus Status, string? Message, SurveyImageDto? Image);
