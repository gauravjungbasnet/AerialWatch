using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Thermal + colour twins (D-039): which camera took each photo, the file-name key shared by a photo and its twin,
    /// and whether its GPS was copied from the twin. Existing photos get their survey's camera and a key from their name.
    /// </summary>
    public partial class AddImagePairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Camera",
                schema: "wfp",
                table: "SurveyImages",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "LocationFromTwin",
                schema: "wfp",
                table: "SurveyImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PairKey",
                schema: "wfp",
                table: "SurveyImages",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            // Existing photos: the survey's camera (as before), and the file name without extension or "_THM" as the key.
            migrationBuilder.Sql(
                "UPDATE i SET i.[Camera] = s.[Camera] FROM [wfp].[SurveyImages] i INNER JOIN [wfp].[Surveys] s ON s.[Id] = i.[SurveyId];");
            migrationBuilder.Sql(
                "UPDATE [wfp].[SurveyImages] SET [PairKey] = LOWER(CASE WHEN CHARINDEX('.', [FileName]) > 0 " +
                "THEN LEFT([FileName], LEN([FileName]) - CHARINDEX('.', REVERSE([FileName]))) ELSE [FileName] END);");
            migrationBuilder.Sql(
                "UPDATE [wfp].[SurveyImages] SET [PairKey] = LEFT([PairKey], LEN([PairKey]) - 4) WHERE [PairKey] LIKE '%[_]thm';");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyImages_SurveyId_PairKey",
                schema: "wfp",
                table: "SurveyImages",
                columns: new[] { "SurveyId", "PairKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyImages_SurveyId_PairKey",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "Camera",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "LocationFromTwin",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "PairKey",
                schema: "wfp",
                table: "SurveyImages");
        }
    }
}
