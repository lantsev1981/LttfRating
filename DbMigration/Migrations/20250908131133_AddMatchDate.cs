using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_IsPending",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsPending",
                table: "Matches");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Date",
                table: "Matches",
                column: "Date");
            
            // Самый простой и эффективный вариант для PostgreSQL
            migrationBuilder.Sql("""
                                 UPDATE "Matches" m
                                 SET "Date" = last_set."Date"
                                 FROM (
                                     SELECT
                                         s."MatchId",
                                         MAX(s."Date") AS "Date",
                                         COUNT(*) FILTER (WHERE s."WinnerLogin" = last_winner."WinnerLogin") AS wins
                                     FROM "Sets" s
                                     CROSS JOIN LATERAL (
                                         SELECT "WinnerLogin"
                                         FROM "Sets" s2
                                         WHERE s2."MatchId" = s."MatchId"
                                         ORDER BY s2."Date" DESC
                                         LIMIT 1
                                     ) last_winner
                                     GROUP BY s."MatchId", last_winner."WinnerLogin"
                                 ) last_set
                                 WHERE m."Id" = last_set."MatchId"
                                 AND m."SetWonCount" = last_set.wins;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_Date",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Matches");

            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_IsPending",
                table: "Matches",
                column: "IsPending");
        }
    }
}
