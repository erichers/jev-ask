using System;
using JevAsk.Api.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JevAsk.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(MySqlSchema.CreateFunAsks);
                migrationBuilder.Sql(MySqlSchema.CreateFunCards);
                migrationBuilder.Sql(MySqlSchema.CreateFunAsksIndex);
                migrationBuilder.Sql(MySqlSchema.CreateFunCardsIndex);
                return;
            }

            migrationBuilder.CreateTable(
                name: "FunAsks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Likelihood = table.Column<double>(type: "REAL", nullable: false),
                    Reasoning = table.Column<string>(type: "longtext", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunAsks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunCards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Likelihood = table.Column<int>(type: "INTEGER", nullable: false),
                    Reasoning = table.Column<string>(type: "longtext", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunCards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunAsks_CreatedAtUtc",
                table: "FunAsks",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FunCards_Category",
                table: "FunCards",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FunAsks");

            migrationBuilder.DropTable(
                name: "FunCards");
        }
    }
}
