using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PvpAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesRemoveUnusedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Spec",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MapName",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "ArenaZone",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArenaZone",
                table: "Matches");

            migrationBuilder.AddColumn<string>(
                name: "Spec",
                table: "Players",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MapName",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
