using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerewolfParty_Server.Migrations
{
    /// <inheritdoc />
    public partial class AddModeratorBadge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "moderator_badge_assigned",
                table: "room",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "moderator_badge_assigned",
                table: "room");
        }
    }
}
