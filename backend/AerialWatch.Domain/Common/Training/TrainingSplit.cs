namespace AerialWatch.Domain.Common.Training;

/// <summary>
/// Which part of an AI training dataset a capture session's checked photos go to (D-038).
/// Shared by all modules. The split is per session (e.g. one drone flight), never per photo, so overlapping
/// neighbouring photos never end up in both training and test.
/// </summary>
public enum TrainingSplit
{
    Train,
    Validation,
    Test
}