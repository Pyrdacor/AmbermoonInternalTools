using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbermoonServer.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreateTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastCodeRequest = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastCodeChangeRequest = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VerificationGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    StateId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_UserState_StateId",
                        column: x => x.StateId,
                        principalTable: "UserState",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Savegame",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    CreateTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Savegame", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Savegame_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Savegame_UserId",
                table: "Savegame",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_StateId",
                table: "User",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserState_Code",
                table: "UserState",
                column: "Code",
                unique: true);

			migrationBuilder.InsertData(
					table: "UserState",
					columns: ["Id", "Name", "Code"],
					values: new object[,]
					{
						{ -2, "Banned", "BANNED" },
					    { -1, "Created but not verified", "CREATED" },
					    { 0, "Verified without any requests", "VERIFIED" },
					    { 1, "Verified and did requests", "ACTIVE" },
					    { 2, "Pending code change request", "CODECHANGE" },					
					});
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DeleteData(
				table: "UserState",
				keyColumn: "Id",
				keyValues: [-1, 0, 1]
			);

            migrationBuilder.DropTable(
                name: "Savegame");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "UserState");
        }
    }
}
