using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <summary>Review tool (D-034): who checked each photo and box, and the "needs a second look" flag.</summary>
    public partial class AddReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Flagged",
                schema: "wfp",
                table: "SurveyImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                schema: "wfp",
                table: "SurveyImages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                schema: "wfp",
                table: "SurveyImages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                schema: "wfp",
                table: "Detections",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                schema: "wfp",
                table: "Detections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flagged",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                schema: "wfp",
                table: "SurveyImages");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "wfp",
                table: "Detections");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                schema: "wfp",
                table: "Detections");
        }
    }
}
