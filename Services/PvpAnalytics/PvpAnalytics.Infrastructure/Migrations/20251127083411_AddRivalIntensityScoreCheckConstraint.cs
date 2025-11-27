using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PvpAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRivalIntensityScoreCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Rival_IntensityScore",
                table: "Rivals",
                sql: "IntensityScore >= 1 AND IntensityScore <= 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Rival_IntensityScore",
                table: "Rivals");
        }
    }
}
