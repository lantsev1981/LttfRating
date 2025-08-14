using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gamers",
                columns: table => new
                {
                    Login = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gamers", x => x.Login);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SetWonCount = table.Column<byte>(type: "smallint", nullable: false),
                    IsPending = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamerMatch",
                columns: table => new
                {
                    Login = table.Column<string>(type: "text", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamerMatch", x => new { x.Login, x.MatchId });
                    table.ForeignKey(
                        name: "FK_GamerMatch_Gamers_Login",
                        column: x => x.Login,
                        principalTable: "Gamers",
                        principalColumn: "Login",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GamerMatch_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sets",
                columns: table => new
                {
                    Num = table.Column<byte>(type: "smallint", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WonPoint = table.Column<byte>(type: "smallint", nullable: false),
                    LostPoint = table.Column<byte>(type: "smallint", nullable: false),
                    WinnerLogin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sets", x => new { x.MatchId, x.Num });
                    table.ForeignKey(
                        name: "FK_Sets_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamerMatch_MatchId",
                table: "GamerMatch",
                column: "MatchId");

            migrationBuilder.Sql(PgsqlHelper.CreateUser(Program.AppUser.Username, Program.AppUser.Password));
            
            migrationBuilder.Sql(PgsqlHelper.GrantPrivileges(
                Program.AppUser.Username,
                PgsqlGrant.SELECT | PgsqlGrant.INSERT | PgsqlGrant.UPDATE,
                "Gamers", "Matches", "GamerMatch", "Sets"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamerMatch");

            migrationBuilder.DropTable(
                name: "Sets");

            migrationBuilder.DropTable(
                name: "Gamers");

            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
