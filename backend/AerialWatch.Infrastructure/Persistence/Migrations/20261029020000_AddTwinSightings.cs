using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Twin sightings (D-041): a pig can be drawn first on the twin photo, before it has a box on the counted photo, so
    /// DetectionId becomes optional and each mark records its counted photo. Existing marks take it from their box.
    /// </summary>
    public partial class AddTwinSightings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TwinMarks_Detections_DetectionId",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.DropIndex(
                name: "IX_TwinMarks_DetectionId",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.AlterColumn<Guid>(
                name: "DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "CountedImageId",
                schema: "wfp",
                table: "TwinMarks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "NotVisibleOnCounted",
                schema: "wfp",
                table: "TwinMarks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE m SET m.[CountedImageId] = d.[ImageId] FROM [wfp].[TwinMarks] m INNER JOIN [wfp].[Detections] d ON d.[Id] = m.[DetectionId];");

            migrationBuilder.CreateIndex(
                name: "IX_TwinMarks_CountedImageId",
                schema: "wfp",
                table: "TwinMarks",
                column: "CountedImageId");

            migrationBuilder.CreateIndex(
                name: "IX_TwinMarks_DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                column: "DetectionId",
                unique: true,
                filter: "[DetectionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TwinMarks_Detections_DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                column: "DetectionId",
                principalSchema: "wfp",
                principalTable: "Detections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [wfp].[TwinMarks] WHERE [DetectionId] IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_TwinMarks_Detections_DetectionId",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.DropIndex(
                name: "IX_TwinMarks_CountedImageId",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.DropIndex(
                name: "IX_TwinMarks_DetectionId",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.DropColumn(
                name: "CountedImageId",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.DropColumn(
                name: "NotVisibleOnCounted",
                schema: "wfp",
                table: "TwinMarks");

            migrationBuilder.AlterColumn<Guid>(
                name: "DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwinMarks_DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                column: "DetectionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TwinMarks_Detections_DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                column: "DetectionId",
                principalSchema: "wfp",
                principalTable: "Detections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
