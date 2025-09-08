using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchDateUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_Date",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Date",
                table: "Matches",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_Date",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Date",
                table: "Matches",
                column: "Date");
        }
    }
}
