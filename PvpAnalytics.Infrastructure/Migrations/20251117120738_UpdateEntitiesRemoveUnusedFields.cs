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
            // Note: MapName and Spec in Players remain from InitialCreate
            // This migration only adds ArenaZone to Matches
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
        }
    }
}
