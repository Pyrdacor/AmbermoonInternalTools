using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbermoonServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSavegameFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Source",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Language",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "GameVersion",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateIndex(
                name: "IX_Savegame_GameVersionId",
                table: "Savegame",
                column: "GameVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Savegame_LanguageId",
                table: "Savegame",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Savegame_SourceId",
                table: "Savegame",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Savegame_GameVersion_GameVersionId",
                table: "Savegame",
                column: "GameVersionId",
                principalTable: "GameVersion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Savegame_Language_LanguageId",
                table: "Savegame",
                column: "LanguageId",
                principalTable: "Language",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Savegame_Source_SourceId",
                table: "Savegame",
                column: "SourceId",
                principalTable: "Source",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Savegame_GameVersion_GameVersionId",
                table: "Savegame");

            migrationBuilder.DropForeignKey(
                name: "FK_Savegame_Language_LanguageId",
                table: "Savegame");

            migrationBuilder.DropForeignKey(
                name: "FK_Savegame_Source_SourceId",
                table: "Savegame");

            migrationBuilder.DropIndex(
                name: "IX_Savegame_GameVersionId",
                table: "Savegame");

            migrationBuilder.DropIndex(
                name: "IX_Savegame_LanguageId",
                table: "Savegame");

            migrationBuilder.DropIndex(
                name: "IX_Savegame_SourceId",
                table: "Savegame");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Source",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Language",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "GameVersion",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);
        }
    }
}
