using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PvpAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityRankings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RankingType = table.Column<string>(type: "text", nullable: false),
                    Period = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: true),
                    PlayerId = table.Column<long>(type: "bigint", nullable: true),
                    TeamId = table.Column<long>(type: "bigint", nullable: true),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRankings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityRankings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityRankings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint(
                        name: "CK_CommunityRankings_PlayerOrTeam",
                        sql: "\"PlayerId\" IS NOT NULL OR \"TeamId\" IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "FeaturedMatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    FeaturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CuratorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Upvotes = table.Column<int>(type: "integer", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturedMatches_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchDiscussionThreads",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchDiscussionThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchDiscussionThreads_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchDiscussionPosts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ThreadId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ParentPostId = table.Column<long>(type: "bigint", nullable: true),
                    Upvotes = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchDiscussionPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchDiscussionPosts_MatchDiscussionPosts_ParentPostId",
                        column: x => x.ParentPostId,
                        principalTable: "MatchDiscussionPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchDiscussionPosts_MatchDiscussionThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "MatchDiscussionThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRankings_PlayerId",
                table: "CommunityRankings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRankings_RankingType_Period_Rank",
                table: "CommunityRankings",
                columns: new[] { "RankingType", "Period", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRankings_TeamId",
                table: "CommunityRankings",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedMatches_FeaturedAt",
                table: "FeaturedMatches",
                column: "FeaturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedMatches_MatchId",
                table: "FeaturedMatches",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchDiscussionPosts_ParentPostId",
                table: "MatchDiscussionPosts",
                column: "ParentPostId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchDiscussionPosts_ThreadId",
                table: "MatchDiscussionPosts",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchDiscussionThreads_MatchId",
                table: "MatchDiscussionThreads",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityRankings");

            migrationBuilder.DropTable(
                name: "FeaturedMatches");

            migrationBuilder.DropTable(
                name: "MatchDiscussionPosts");

            migrationBuilder.DropTable(
                name: "MatchDiscussionThreads");
        }
    }
}
