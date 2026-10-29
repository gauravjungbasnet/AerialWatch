using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Persistence.Migrations
{
    /// <summary>
    /// Data migration: inserts the published Hvala et al. (2023) coefficients as the first row of wfp.ModelCoefficientSets,
    /// so `dotnet ef database update` alone gives a ready database. Values must equal Hvala2023Coefficients.Published
    /// (checked by MigrationsTests.Seed_migration_matches_published_coefficients).
    /// </summary>
    public partial class SeedPublishedCoefficients : Migration
    {
        public static readonly Guid SeedId = new("5eed2023-0000-4000-8000-000000000001");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "wfp",
                table: "ModelCoefficientSets",
                columns: new[]
                {
                    "Id", "Version", "IsPublished", "CreatedBy",
                    "ThermalIntercept", "ThermalSeasonDry", "ThermalMidAfternoon", "ThermalMidMorning", "ThermalEarlyMorning",
                    "RgbIntercept", "RgbSeasonDry", "RgbMidAfternoon", "RgbMidMorning", "RgbEarlyMorning",
                    "ThermalEnvIntercept", "ThermalEnvTemperatureC", "ThermalEnvCanopy",
                    "RgbEnvIntercept", "RgbEnvTemperatureC", "RgbEnvCanopy",
                    "CreatedAt", "UpdatedAt"
                },
                values: new object[]
                {
                    SeedId, "hvala2023-v1", true, "system",
                    0.951, -1.258, -2.728, -1.043, -0.427,
                    2.335, -1.8016, -3.729, -2.3308, -2.811,
                    5.869, -0.175, -3.442,
                    1.2789, null, -3.9433,
                    new DateTimeOffset(2026, 10, 24, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 24, 0, 0, 0, TimeSpan.Zero)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "wfp",
                table: "ModelCoefficientSets",
                keyColumn: "Id",
                keyValue: SeedId);
        }
    }
}
