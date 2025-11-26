using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PvpAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoritePlayers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoritePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoritePlayers_Players_TargetPlayerId",
                        column: x => x.TargetPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rivals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpponentPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IntensityScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rivals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rivals_Players_OpponentPlayerId",
                        column: x => x.OpponentPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeType = table.Column<string>(type: "text", nullable: false),
                    BadgeName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePlayers_OwnerUserId",
                table: "FavoritePlayers",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePlayers_OwnerUserId_TargetPlayerId",
                table: "FavoritePlayers",
                columns: new[] { "OwnerUserId", "TargetPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePlayers_TargetPlayerId",
                table: "FavoritePlayers",
                column: "TargetPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rivals_OpponentPlayerId",
                table: "Rivals",
                column: "OpponentPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rivals_OwnerUserId",
                table: "Rivals",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rivals_OwnerUserId_OpponentPlayerId",
                table: "Rivals",
                columns: new[] { "OwnerUserId", "OpponentPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId",
                table: "UserBadges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoritePlayers");

            migrationBuilder.DropTable(
                name: "Rivals");

            migrationBuilder.DropTable(
                name: "UserBadges");
        }
    }
}
