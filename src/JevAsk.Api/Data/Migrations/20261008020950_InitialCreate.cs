using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JevAsk.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    CREATE TABLE `Questions` (
                        `Id` bigint NOT NULL AUTO_INCREMENT,
                        `Question` varchar(500) NOT NULL,
                        `Ticker` varchar(16) NOT NULL,
                        `Probability` double NOT NULL,
                        `PayloadJson` longtext NOT NULL,
                        `CreatedAtUtc` datetime(6) NOT NULL,
                        PRIMARY KEY (`Id`)
                    ) CHARACTER SET utf8mb4;
                    """);
                migrationBuilder.Sql("""
                    CREATE TABLE `Series` (
                        `Ticker` varchar(16) NOT NULL,
                        `BarsJson` longtext NOT NULL,
                        `Origin` varchar(64) NOT NULL,
                        `StoredAtUtc` datetime(6) NOT NULL,
                        PRIMARY KEY (`Ticker`)
                    ) CHARACTER SET utf8mb4;
                    """);
                migrationBuilder.Sql("CREATE INDEX `IX_Questions_CreatedAtUtc` ON `Questions` (`CreatedAtUtc`);");
                return;
            }

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Probability = table.Column<double>(type: "REAL", nullable: false),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    BarsJson = table.Column<string>(type: "longtext", nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Ticker);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedAtUtc",
                table: "Questions",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Questions");
            migrationBuilder.DropTable(name: "Series");
        }
    }
}
