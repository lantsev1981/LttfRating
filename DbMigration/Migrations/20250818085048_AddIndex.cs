using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sets_Date",
                table: "Sets",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sets_WinnerLogin",
                table: "Sets",
                column: "WinnerLogin");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_IsPending",
                table: "Matches",
                column: "IsPending");

            migrationBuilder.CreateIndex(
                name: "IX_Gamers_Rating",
                table: "Gamers",
                column: "Rating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sets_Date",
                table: "Sets");

            migrationBuilder.DropIndex(
                name: "IX_Sets_WinnerLogin",
                table: "Sets");

            migrationBuilder.DropIndex(
                name: "IX_Matches_IsPending",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Gamers_Rating",
                table: "Gamers");
        }
    }
}
