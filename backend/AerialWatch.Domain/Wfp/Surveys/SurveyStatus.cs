namespace AerialWatch.Domain.Wfp.Surveys;

/// <summary>Lifecycle of a survey (docs/04 §5.4).</summary>
public enum SurveyStatus
{
    Draft,
    Uploading,
    Processing,
    InReview,
    Reviewed,
    Failed
}
