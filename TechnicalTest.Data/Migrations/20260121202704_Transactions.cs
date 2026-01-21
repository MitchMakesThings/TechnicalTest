using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnicalTest.Data.Migrations
{
    /// <inheritdoc />
    public partial class Transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromBankAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToBankAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "sysdatetimeoffset()"),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "sysdatetimeoffset()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_BankAccounts_FromBankAccountId",
                        column: x => x.FromBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_BankAccounts_ToBankAccountId",
                        column: x => x.ToBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FromBankAccountId",
                table: "Transactions",
                column: "FromBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ToBankAccountId",
                table: "Transactions",
                column: "ToBankAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
