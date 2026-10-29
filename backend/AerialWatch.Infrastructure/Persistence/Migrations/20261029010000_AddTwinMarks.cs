using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <summary>Twin check (D-040): where each confirmed pig is on the other camera's photo, or that it isn't visible there.</summary>
    public partial class AddTwinMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TwinMarks",
                schema: "wfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TwinImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    X = table.Column<double>(type: "float", nullable: true),
                    Y = table.Column<double>(type: "float", nullable: true),
                    Width = table.Column<double>(type: "float", nullable: true),
                    Height = table.Column<double>(type: "float", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwinMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TwinMarks_Detections_DetectionId",
                        column: x => x.DetectionId,
                        principalSchema: "wfp",
                        principalTable: "Detections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TwinMarks_DetectionId",
                schema: "wfp",
                table: "TwinMarks",
                column: "DetectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwinMarks_TwinImageId",
                schema: "wfp",
                table: "TwinMarks",
                column: "TwinImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwinMarks",
                schema: "wfp");
        }
    }
}
