namespace AerialWatch.Application.Wfp.SurveyImages;

/// <summary>Storage layout for survey photos: wfp/surveys/{surveyId}/originals|thumbs/{imageId}.ext (D-030).</summary>
public static class SurveyImageStorageKeys
{
    public static string SurveyFolder(Guid surveyId) => $"wfp/surveys/{surveyId:D}";

    public static string Original(Guid surveyId, Guid imageId, string extension) =>
        $"{SurveyFolder(surveyId)}/originals/{imageId:D}{extension.ToLowerInvariant()}";

    public static string Thumbnail(Guid surveyId, Guid imageId) => $"{SurveyFolder(surveyId)}/thumbs/{imageId:D}.jpg";
}
