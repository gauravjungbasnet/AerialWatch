namespace AerialWatch.Contracts.Wfp.SurveyImages.Models;

/// <summary>What happened to one file of an upload batch.</summary>
public enum UploadFileStatus
{
    /// <summary>Saved and added to the survey.</summary>
    Uploaded,

    /// <summary>Skipped: the same photo is already in this survey (or earlier in the same batch).</summary>
    Duplicate,

    /// <summary>Not accepted (wrong type, too big, not a readable image…); see Message.</summary>
    Rejected
}
