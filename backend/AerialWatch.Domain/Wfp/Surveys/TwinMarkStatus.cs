namespace AerialWatch.Domain.Wfp.Surveys;

/// <summary>Ranger's answer when checking a confirmed pig on the other camera's photo (D-040).</summary>
public enum TwinMarkStatus
{
    /// <summary>The pig is visible on the twin; X/Y/Width/Height give where.</summary>
    Confirmed,

    /// <summary>The pig can't be seen on the twin (e.g. under canopy in colour, or cool ground in thermal).</summary>
    NotVisible
}
