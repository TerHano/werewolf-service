using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WerewolfParty_Server.Migrations
{
    /// <inheritdoc />
    public partial class ini : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    game_role = table.Column<int>(type: "integer", nullable: false),
                    is_alive = table.Column<bool>(type: "boolean", nullable: false),
                    night_killed = table.Column<int>(type: "integer", nullable: false),
                    player_room_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_room",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nickname = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    avatar_index = table.Column<int>(type: "integer", nullable: false),
                    player_status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_room", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "room",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    current_moderator = table.Column<int>(type: "integer", nullable: true),
                    game_state = table.Column<int>(type: "integer", nullable: false),
                    current_night = table.Column<int>(type: "integer", nullable: false),
                    is_day = table.Column<bool>(type: "boolean", nullable: false),
                    win_condition = table.Column<int>(type: "integer", nullable: false),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room", x => x.id);
                    table.ForeignKey(
                        name: "FK_room_player_room_current_moderator",
                        column: x => x.current_moderator,
                        principalTable: "player_room",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "role_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    number_of_werewolves = table.Column<int>(type: "integer", nullable: false),
                    selected_roles = table.Column<int[]>(type: "integer[]", nullable: false),
                    show_game_summary = table.Column<bool>(type: "boolean", nullable: false),
                    allow_multiple_self_heals = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_settings_room_room_id",
                        column: x => x.room_id,
                        principalTable: "room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_game_action",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<string>(type: "text", nullable: false),
                    player_role_id = table.Column<int>(type: "integer", nullable: true),
                    action_type = table.Column<int>(type: "integer", nullable: false),
                    affected_player_role_id = table.Column<int>(type: "integer", nullable: false),
                    night = table.Column<int>(type: "integer", nullable: false),
                    action_state = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_game_action", x => x.id);
                    table.ForeignKey(
                        name: "FK_room_game_action_player_role_affected_player_role_id",
                        column: x => x.affected_player_role_id,
                        principalTable: "player_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_game_action_player_role_player_role_id",
                        column: x => x.player_role_id,
                        principalTable: "player_role",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_room_game_action_room_room_id",
                        column: x => x.room_id,
                        principalTable: "room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_role_player_room_id",
                table: "player_role",
                column: "player_room_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_room_room_id",
                table: "player_room",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_settings_room_id",
                table: "role_settings",
                column: "room_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_current_moderator",
                table: "room",
                column: "current_moderator");

            migrationBuilder.CreateIndex(
                name: "IX_room_game_action_affected_player_role_id",
                table: "room_game_action",
                column: "affected_player_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_game_action_player_role_id",
                table: "room_game_action",
                column: "player_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_game_action_room_id",
                table: "room_game_action",
                column: "room_id");

            migrationBuilder.AddForeignKey(
                name: "FK_player_role_player_room_player_room_id",
                table: "player_role",
                column: "player_room_id",
                principalTable: "player_room",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_player_room_room_room_id",
                table: "player_room",
                column: "room_id",
                principalTable: "room",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_room_player_room_current_moderator",
                table: "room");

            migrationBuilder.DropTable(
                name: "role_settings");

            migrationBuilder.DropTable(
                name: "room_game_action");

            migrationBuilder.DropTable(
                name: "player_role");

            migrationBuilder.DropTable(
                name: "player_room");

            migrationBuilder.DropTable(
                name: "room");
        }
    }
}
