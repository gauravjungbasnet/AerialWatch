namespace AerialWatch.Contracts.Wfp.SurveyImages.Models;

/// <summary>
/// One file from an upload request, independent of ASP.NET (the controller maps IFormFile to this).
/// Content must be seekable: it is read more than once (type check, hash, save).
/// </summary>
public sealed record UploadedFile(string FileName, long Length, Stream Content);
