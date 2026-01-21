using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnicalTest.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInitialModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysdatetimeoffset()");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyLimit",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysdatetimeoffset()");

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "BankAccounts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BankAccounts",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysdatetimeoffset()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "BankAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FrozenAt",
                table: "BankAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "BankAccounts",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysdatetimeoffset()");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_BankAccounts_AccountNumber",
                table: "BankAccounts",
                column: "AccountNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_BankAccounts_AccountNumber",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DailyLimit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "FrozenAt",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BankAccounts");
        }
    }
}
