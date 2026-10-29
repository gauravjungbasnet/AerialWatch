using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wfp");

            migrationBuilder.CreateTable(
                name: "CoefficientTypes",
                schema: "wfp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Camera = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoefficientTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelCoefficientSets",
                schema: "wfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelCoefficientSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Surveys",
                schema: "wfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SurveyDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Camera = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimeWindow = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AltitudeM = table.Column<double>(type: "float", nullable: true),
                    AreaKm2 = table.Column<double>(type: "float", nullable: true),
                    GroundCover = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelCoefficientValues",
                schema: "wfp",
                columns: table => new
                {
                    SetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoefficientTypeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelCoefficientValues", x => new { x.SetId, x.CoefficientTypeId });
                    table.ForeignKey(
                        name: "FK_ModelCoefficientValues_CoefficientTypes_CoefficientTypeId",
                        column: x => x.CoefficientTypeId,
                        principalSchema: "wfp",
                        principalTable: "CoefficientTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelCoefficientValues_ModelCoefficientSets_SetId",
                        column: x => x.SetId,
                        principalSchema: "wfp",
                        principalTable: "ModelCoefficientSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyImages",
                schema: "wfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    WidthPx = table.Column<int>(type: "int", nullable: true),
                    HeightPx = table.Column<int>(type: "int", nullable: true),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Reviewed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyImages_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalSchema: "wfp",
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detections",
                schema: "wfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false),
                    Width = table.Column<double>(type: "float", nullable: false),
                    Height = table.Column<double>(type: "float", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detections_SurveyImages_ImageId",
                        column: x => x.ImageId,
                        principalSchema: "wfp",
                        principalTable: "SurveyImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoefficientTypes_Camera_Model_Term",
                schema: "wfp",
                table: "CoefficientTypes",
                columns: new[] { "Camera", "Model", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoefficientTypes_Code",
                schema: "wfp",
                table: "CoefficientTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detections_ImageId_Status",
                schema: "wfp",
                table: "Detections",
                columns: new[] { "ImageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelCoefficientSets_CreatedAt",
                schema: "wfp",
                table: "ModelCoefficientSets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelCoefficientValues_CoefficientTypeId",
                schema: "wfp",
                table: "ModelCoefficientValues",
                column: "CoefficientTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyImages_SurveyId",
                schema: "wfp",
                table: "SurveyImages",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_CreatedAt",
                schema: "wfp",
                table: "Surveys",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detections",
                schema: "wfp");

            migrationBuilder.DropTable(
                name: "ModelCoefficientValues",
                schema: "wfp");

            migrationBuilder.DropTable(
                name: "SurveyImages",
                schema: "wfp");

            migrationBuilder.DropTable(
                name: "CoefficientTypes",
                schema: "wfp");

            migrationBuilder.DropTable(
                name: "ModelCoefficientSets",
                schema: "wfp");

            migrationBuilder.DropTable(
                name: "Surveys",
                schema: "wfp");
        }
    }
}
