namespace AerialWatch.Contracts.Wfp.SurveyImages.Models;

/// <summary>An image file to send to the browser. The caller disposes Content.</summary>
public sealed record ImageFile(Stream Content, string ContentType, string FileName);
