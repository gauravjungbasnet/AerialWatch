namespace AerialWatch.Contracts.Wfp.Review.Models;

/// <summary>Which photos the review queue shows.</summary>
public enum ReviewImageFilter
{
    All,
    /// <summary>Not yet marked as checked.</summary>
    NotReviewed,
    /// <summary>Has AI suggestions nobody has accepted or rejected yet.</summary>
    Pending,
    /// <summary>At least one confirmed pig.</summary>
    WithPigs,
    /// <summary>Checked, and no confirmed pig.</summary>
    NoPigs,
    /// <summary>Marked "needs a second look".</summary>
    Flagged
}
