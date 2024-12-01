using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbermoonServer.Migrations
{
    /// <inheritdoc />
    public partial class DataAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Language_Code",
                table: "Language");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Language");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Language",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Language_Code",
                table: "Language",
                column: "Code",
                unique: true);
        }
    }
}
