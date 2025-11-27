using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixIsProfilePublicDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter the default value for IsProfilePublic to match the entity default (true)
            migrationBuilder.AlterColumn<bool>(
                name: "IsProfilePublic",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the old default value (false)
            migrationBuilder.AlterColumn<bool>(
                name: "IsProfilePublic",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
