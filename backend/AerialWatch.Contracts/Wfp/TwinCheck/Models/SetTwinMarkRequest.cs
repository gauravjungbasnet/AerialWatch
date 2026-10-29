namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>Confirm (with the box on the twin, in its original pixels) or mark as not visible.</summary>
public sealed record SetTwinMarkRequest(TwinMarkState State, double? X = null, double? Y = null, double? Width = null, double? Height = null);
