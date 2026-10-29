using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Data migration, so `dotnet ef database update` alone gives a ready database:
    ///  1. wfp.CoefficientTypes: the 15 coefficients of the Hvala et al. (2023) models (must equal CoefficientCatalog.All).
    ///  2. wfp.ModelCoefficientSets + wfp.ModelCoefficientValues: the published values as the first saved set
    ///     (must equal Hvala2023Coefficients.Published).
    /// Both are checked by MigrationsTests.Seed_migration_matches_catalog_and_published_coefficients.
    /// </summary>
    public partial class SeedPublishedCoefficients : Migration
    {
        public static readonly Guid SeedSetId = new("5eed2023-0000-4000-8000-000000000001");

        private static readonly DateTimeOffset SeededAt = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "wfp",
                table: "CoefficientTypes",
                columns: new[] { "Id", "Code", "Camera", "Model", "Term", "Label", "Description", "SortOrder" },
                values: new object[,]
                {
                    { 1, "thermal.timeSeason.intercept", "Thermal", "TimeSeason", "Intercept", "Baseline", "Early evening, wet season", 10 },
                    { 2, "thermal.timeSeason.seasonDry", "Thermal", "TimeSeason", "SeasonDry", "Dry season", "Added in the dry season", 20 },
                    { 3, "thermal.timeSeason.midAfternoon", "Thermal", "TimeSeason", "MidAfternoon", "Mid-afternoon", "Compared with early evening", 30 },
                    { 4, "thermal.timeSeason.midMorning", "Thermal", "TimeSeason", "MidMorning", "Mid-morning", "Compared with early evening", 40 },
                    { 5, "thermal.timeSeason.earlyMorning", "Thermal", "TimeSeason", "EarlyMorning", "Early morning", "Compared with early evening", 50 },
                    { 6, "rgb.timeSeason.intercept", "Rgb", "TimeSeason", "Intercept", "Baseline", "Early evening, wet season", 60 },
                    { 7, "rgb.timeSeason.seasonDry", "Rgb", "TimeSeason", "SeasonDry", "Dry season", "Added in the dry season", 70 },
                    { 8, "rgb.timeSeason.midAfternoon", "Rgb", "TimeSeason", "MidAfternoon", "Mid-afternoon", "Compared with early evening", 80 },
                    { 9, "rgb.timeSeason.midMorning", "Rgb", "TimeSeason", "MidMorning", "Mid-morning", "Compared with early evening", 90 },
                    { 10, "rgb.timeSeason.earlyMorning", "Rgb", "TimeSeason", "EarlyMorning", "Early morning", "Compared with early evening", 100 },
                    { 11, "thermal.environment.intercept", "Thermal", "Environment", "Intercept", "Baseline", "Open ground", 110 },
                    { 12, "thermal.environment.ambientTemperatureC", "Thermal", "Environment", "AmbientTemperatureC", "Air temperature", "Per °C", 120 },
                    { 13, "thermal.environment.timeUnderCanopy", "Thermal", "Environment", "TimeUnderCanopy", "Time under canopy", "Per unit, 0 to 1", 130 },
                    { 14, "rgb.environment.intercept", "Rgb", "Environment", "Intercept", "Baseline", "Open ground", 140 },
                    { 15, "rgb.environment.timeUnderCanopy", "Rgb", "Environment", "TimeUnderCanopy", "Time under canopy", "Per unit, 0 to 1", 150 },
                });

            migrationBuilder.InsertData(
                schema: "wfp",
                table: "ModelCoefficientSets",
                columns: new[] { "Id", "Version", "IsPublished", "CreatedBy", "CreatedAt", "UpdatedAt" },
                values: new object[] { SeedSetId, "hvala2023-v1", true, "system", SeededAt, SeededAt });

            migrationBuilder.InsertData(
                schema: "wfp",
                table: "ModelCoefficientValues",
                columns: new[] { "SetId", "CoefficientTypeId", "Value" },
                values: new object[,]
                {
                    { SeedSetId, 1, 0.951 }, // thermal.timeSeason.intercept
                    { SeedSetId, 2, -1.258 }, // thermal.timeSeason.seasonDry
                    { SeedSetId, 3, -2.728 }, // thermal.timeSeason.midAfternoon
                    { SeedSetId, 4, -1.043 }, // thermal.timeSeason.midMorning
                    { SeedSetId, 5, -0.427 }, // thermal.timeSeason.earlyMorning
                    { SeedSetId, 6, 2.335 }, // rgb.timeSeason.intercept
                    { SeedSetId, 7, -1.8016 }, // rgb.timeSeason.seasonDry
                    { SeedSetId, 8, -3.729 }, // rgb.timeSeason.midAfternoon
                    { SeedSetId, 9, -2.3308 }, // rgb.timeSeason.midMorning
                    { SeedSetId, 10, -2.811 }, // rgb.timeSeason.earlyMorning
                    { SeedSetId, 11, 5.869 }, // thermal.environment.intercept
                    { SeedSetId, 12, -0.175 }, // thermal.environment.ambientTemperatureC
                    { SeedSetId, 13, -3.442 }, // thermal.environment.timeUnderCanopy
                    { SeedSetId, 14, 1.2789 }, // rgb.environment.intercept
                    { SeedSetId, 15, -3.9433 }, // rgb.environment.timeUnderCanopy
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Values of every set point at the types, so they all go first (only possible on a database you are resetting).
            migrationBuilder.Sql("DELETE FROM [wfp].[ModelCoefficientValues];");
            migrationBuilder.Sql("DELETE FROM [wfp].[ModelCoefficientSets];");
            migrationBuilder.Sql("DELETE FROM [wfp].[CoefficientTypes];");
        }
    }
}
