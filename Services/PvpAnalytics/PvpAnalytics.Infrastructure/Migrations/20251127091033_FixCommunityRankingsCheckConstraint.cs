using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PvpAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCommunityRankingsCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CommunityRankings_PlayerOrTeam",
                table: "CommunityRankings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CommunityRankings_PlayerOrTeam",
                table: "CommunityRankings",
                sql: "PlayerId IS NOT NULL OR TeamId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CommunityRankings_PlayerOrTeam",
                table: "CommunityRankings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CommunityRankings_PlayerOrTeam",
                table: "CommunityRankings",
                sql: "\"PlayerId\" IS NOT NULL OR \"TeamId\" IS NOT NULL");
        }
    }
}
