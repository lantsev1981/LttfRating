using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Gamers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gamers_UserId",
                table: "Gamers",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Gamers_UserId",
                table: "Gamers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Gamers");
        }
    }
}
