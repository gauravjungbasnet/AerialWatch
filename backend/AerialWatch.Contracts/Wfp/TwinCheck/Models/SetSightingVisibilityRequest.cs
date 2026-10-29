namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>Mark a twin sighting as not visible on the counted photo, or undo that (D-041).</summary>
public sealed record SetSightingVisibilityRequest(bool NotVisibleOnCounted);
