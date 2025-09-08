using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class DelUpdateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdateType",
                table: "TelegramInputs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UpdateType",
                table: "TelegramInputs",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");
        }
    }
}
