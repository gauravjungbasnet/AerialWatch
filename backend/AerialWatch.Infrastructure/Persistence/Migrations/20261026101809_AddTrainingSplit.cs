using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrainingSplit",
                schema: "wfp",
                table: "Surveys",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingSplit",
                schema: "wfp",
                table: "Surveys");
        }
    }
}
