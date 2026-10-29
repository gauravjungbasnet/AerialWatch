using AerialWatch.Domain.Wfp.Surveys;

namespace AerialWatch.Application.Wfp.Review;

/// <summary>Review rules (D-034). The frontend mirrors MinBoxSizePx and the survey states.</summary>
public static class ReviewRules
{
    /// <summary>Smallest box side, in original pixels; smaller drags are treated as accidental clicks.</summary>
    public const double MinBoxSizePx = 4;

    /// <summary>Boxes may reach this far past the photo's edge (rounding when converting screen to image pixels).</summary>
    public const double EdgeTolerancePx = 1;

    /// <summary>Review can start once photos are uploaded (or the AI has finished processing them).</summary>
    public static bool CanStart(SurveyStatus status) => status is SurveyStatus.Uploading or SurveyStatus.Processing;

    /// <summary>Boxes and photo flags can change only while the review is open.</summary>
    public static bool AcceptsChanges(SurveyStatus status) => status == SurveyStatus.InReview;

    public static string NotEditableMessage(SurveyStatus status) => status switch
    {
        SurveyStatus.Reviewed => "This review is complete. Reopen it to make changes.",
        SurveyStatus.Draft or SurveyStatus.Uploading or SurveyStatus.Processing => "Start the review first.",
        _ => $"The survey can't be reviewed while it is {status}.",
    };

    /// <summary>Confirmed pigs = accepted boxes, whether the AI suggested them or a ranger drew them.</summary>
    public static bool IsConfirmed(DetectionStatus status) => status == DetectionStatus.Accepted;
}
