using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnicalTest.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BankAccounts_FromBankAccountId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BankAccounts_ToBankAccountId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "ToBankAccountId",
                table: "Transactions",
                newName: "DebitBankAccountId");

            migrationBuilder.RenameColumn(
                name: "FromBankAccountId",
                table: "Transactions",
                newName: "CreditBankAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ToBankAccountId",
                table: "Transactions",
                newName: "IX_Transactions_DebitBankAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_FromBankAccountId",
                table: "Transactions",
                newName: "IX_Transactions_CreditBankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BankAccounts_CreditBankAccountId",
                table: "Transactions",
                column: "CreditBankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BankAccounts_DebitBankAccountId",
                table: "Transactions",
                column: "DebitBankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BankAccounts_CreditBankAccountId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BankAccounts_DebitBankAccountId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "DebitBankAccountId",
                table: "Transactions",
                newName: "ToBankAccountId");

            migrationBuilder.RenameColumn(
                name: "CreditBankAccountId",
                table: "Transactions",
                newName: "FromBankAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_DebitBankAccountId",
                table: "Transactions",
                newName: "IX_Transactions_ToBankAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CreditBankAccountId",
                table: "Transactions",
                newName: "IX_Transactions_FromBankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BankAccounts_FromBankAccountId",
                table: "Transactions",
                column: "FromBankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BankAccounts_ToBankAccountId",
                table: "Transactions",
                column: "ToBankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
