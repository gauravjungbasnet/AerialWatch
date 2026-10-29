namespace AerialWatch.Persistence;

/// <summary>
/// SQL Server schemas, one per app module (D-026). Tables never go in dbo; dbo only holds EF's __EFMigrationsHistory.
/// See docs/08-database-schema.md.
/// </summary>
public static class Schemas
{
    /// <summary>Watch Feral Pigs: surveys, photos, detections, research-model coefficients.</summary>
    public const string Wfp = "wfp";

    // Reserved, created by the first migration of each module:
    //   wwl  – Watch Wetlands
    //   wgn  – Watch Ghost Net
    //   core – shared across modules (users, roles, audit) when sign-in is added
}
