using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Modules.DTOs;

public record TransactionDto(string DebitAccountNumber, string CreditAccountNumber, decimal Amount, string? Reference)
{
    public TransactionDto(Transaction transaction) : this(
        transaction.DebitBankAccount!.AccountNumber,
        transaction.CreditBankAccount!.AccountNumber,
        transaction.Amount,
        transaction.Reference
    )
    {
    }
}