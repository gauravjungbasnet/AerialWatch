using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyImageFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyImages_SurveyId",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                schema: "wfp",
                table: "SurveyImages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                schema: "wfp",
                table: "SurveyImages",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                schema: "wfp",
                table: "SurveyImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyImages_SurveyId_Sha256",
                schema: "wfp",
                table: "SurveyImages",
                columns: new[] { "SurveyId", "Sha256" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyImages_SurveyId_Sha256",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "ContentType",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "Sha256",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyImages_SurveyId",
                schema: "wfp",
                table: "SurveyImages",
                column: "SurveyId");
        }
    }
}
