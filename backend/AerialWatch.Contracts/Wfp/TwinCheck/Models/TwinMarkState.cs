namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>Suggested = the app's guess, not checked yet; Confirmed = ranger placed or accepted it; NotVisible = can't be seen on the twin.</summary>
public enum TwinMarkState
{
    Suggested,
    Confirmed,
    NotVisible
}
