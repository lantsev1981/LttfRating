using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddChatIdAndMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChatId",
                table: "Sets",
                type: "bigint",
                nullable: false,
                defaultValue: -1L);

            migrationBuilder.AddColumn<int>(
                name: "MessageId",
                table: "Sets",
                type: "integer",
                nullable: false,
                defaultValue: -1);
            
            migrationBuilder.Sql(PgsqlHelper.GrantPrivileges(
                Program.AppUser.Username,
                PgsqlGrant.DELETE,
                "Sets"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "Sets");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Sets");
        }
    }
}
