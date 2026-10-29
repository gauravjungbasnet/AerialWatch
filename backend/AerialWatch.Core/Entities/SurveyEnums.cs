namespace AerialWatch.Core.Entities;

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

/// <summary>Ranger's description of the ground; used to approximate canopy for the environment model (D-005).</summary>
public enum GroundCover
{
    Unknown,
    Open,
    Mixed,
    Dense
}

public enum DetectionSource
{
    Ai,
    Ranger
}

public enum DetectionStatus
{
    Pending,
    Accepted,
    Rejected
}
