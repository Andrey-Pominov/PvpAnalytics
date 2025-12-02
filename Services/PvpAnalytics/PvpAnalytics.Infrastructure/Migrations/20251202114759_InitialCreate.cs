using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PvpAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UniqueHash = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MapName = table.Column<string>(type: "text", nullable: false),
                    ArenaZone = table.Column<int>(type: "integer", nullable: false),
                    ArenaMatchId = table.Column<string>(type: "text", nullable: true),
                    GameMode = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    IsRanked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Realm = table.Column<string>(type: "text", nullable: false),
                    Class = table.Column<string>(type: "text", nullable: false),
                    Faction = table.Column<string>(type: "text", nullable: false),
                    Spec = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    Bracket = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
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
                name: "CombatLogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourcePlayerId = table.Column<long>(type: "bigint", nullable: false),
                    TargetPlayerId = table.Column<long>(type: "bigint", nullable: true),
                    Ability = table.Column<string>(type: "text", nullable: false),
                    DamageDone = table.Column<int>(type: "integer", nullable: false),
                    HealingDone = table.Column<int>(type: "integer", nullable: false),
                    CrowdControl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatLogEntries_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatLogEntries_Players_SourcePlayerId",
                        column: x => x.SourcePlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombatLogEntries_Players_TargetPlayerId",
                        column: x => x.TargetPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    Team = table.Column<string>(type: "text", nullable: false),
                    RatingBefore = table.Column<int>(type: "integer", nullable: false),
                    RatingAfter = table.Column<int>(type: "integer", nullable: false),
                    IsWinner = table.Column<bool>(type: "boolean", nullable: false),
                    Spec = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResults_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchResults_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.CheckConstraint("CK_Rival_IntensityScore", "\"IntensityScore\" >= 1 AND \"IntensityScore\" <= 10");
                    table.ForeignKey(
                        name: "FK_Rivals_Players_OpponentPlayerId",
                        column: x => x.OpponentPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    table.CheckConstraint("CK_CommunityRankings_PlayerOrTeam", "\"PlayerId\" IS NOT NULL OR \"TeamId\" IS NOT NULL");
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
                });

            migrationBuilder.CreateTable(
                name: "TeamMatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<long>(type: "bigint", nullable: false),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    IsWin = table.Column<bool>(type: "boolean", nullable: false),
                    RatingChange = table.Column<int>(type: "integer", nullable: true),
                    RatingBefore = table.Column<int>(type: "integer", nullable: true),
                    RatingAfter = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMatches_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMatches_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_CombatLogEntries_MatchId",
                table: "CombatLogEntries",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogEntries_SourcePlayerId",
                table: "CombatLogEntries",
                column: "SourcePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogEntries_TargetPlayerId",
                table: "CombatLogEntries",
                column: "TargetPlayerId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UniqueHash",
                table: "Matches",
                column: "UniqueHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_MatchId_PlayerId",
                table: "MatchResults",
                columns: new[] { "MatchId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_PlayerId",
                table: "MatchResults",
                column: "PlayerId");

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
                name: "IX_TeamMatches_MatchId",
                table: "TeamMatches",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMatches_TeamId_MatchId",
                table: "TeamMatches",
                columns: new[] { "TeamId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_PlayerId",
                table: "TeamMembers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_PlayerId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedByUserId",
                table: "Teams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId",
                table: "UserBadges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombatLogEntries");

            migrationBuilder.DropTable(
                name: "CommunityRankings");

            migrationBuilder.DropTable(
                name: "FavoritePlayers");

            migrationBuilder.DropTable(
                name: "FeaturedMatches");

            migrationBuilder.DropTable(
                name: "MatchDiscussionPosts");

            migrationBuilder.DropTable(
                name: "MatchResults");

            migrationBuilder.DropTable(
                name: "Rivals");

            migrationBuilder.DropTable(
                name: "TeamMatches");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropTable(
                name: "MatchDiscussionThreads");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
