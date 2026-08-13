using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerewolfParty_Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNightStepLockIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "night_step_locked_in",
                table: "room",
                type: "integer[]",
                nullable: false,
                // Existing rooms need a value or the ALTER fails on a non-empty table. An empty
                // array is also the correct starting state: nobody has locked in yet.
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "night_step_locked_in",
                table: "room");
        }
    }
}
