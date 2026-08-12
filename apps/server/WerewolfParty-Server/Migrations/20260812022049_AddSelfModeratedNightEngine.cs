using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerewolfParty_Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfModeratedNightEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "night_step",
                table: "room",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "night_step_deadline",
                table: "room",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "night_step_seconds",
                table: "role_settings",
                type: "integer",
                nullable: false,
                defaultValue: 45);

            migrationBuilder.AddColumn<bool>(
                name: "self_moderated",
                table: "role_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "night_step",
                table: "room");

            migrationBuilder.DropColumn(
                name: "night_step_deadline",
                table: "room");

            migrationBuilder.DropColumn(
                name: "night_step_seconds",
                table: "role_settings");

            migrationBuilder.DropColumn(
                name: "self_moderated",
                table: "role_settings");
        }
    }
}
