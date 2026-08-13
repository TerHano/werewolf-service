using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WerewolfParty_Server.Migrations
{
    /// <inheritdoc />
    public partial class LowerDefaultNightStepSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "night_step_seconds",
                table: "role_settings",
                type: "integer",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 45);

            // Existing rooms keep whatever they were given, but until now there was no way to
            // set this — so every stored 45 is the old default rather than a deliberate choice.
            // Move those across so existing rooms get the shorter night too.
            migrationBuilder.Sql(
                "UPDATE role_settings SET night_step_seconds = 20 WHERE night_step_seconds = 45;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "night_step_seconds",
                table: "role_settings",
                type: "integer",
                nullable: false,
                defaultValue: 45,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 20);
        }
    }
}
