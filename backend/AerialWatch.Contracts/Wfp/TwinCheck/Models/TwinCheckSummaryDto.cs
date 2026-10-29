using AerialWatch.Domain.Wfp.ResearchModel;

namespace AerialWatch.Contracts.Wfp.TwinCheck.Models;

/// <summary>
/// Survey-wide twin check (D-040, D-041): of the confirmed pigs on photos that have a twin, how many were checked and how
/// many were visible on the other camera; plus pigs seen only on the other camera (never counted).
/// TwinCamera is null when no counted photo has a twin.
/// </summary>
public sealed record TwinCheckSummaryDto(
    CameraType? TwinCamera,
    int PigsWithTwin,
    int Checked,
    int VisibleOnTwin,
    int NotVisibleOnTwin,
    int SeenOnlyOnTwin);
