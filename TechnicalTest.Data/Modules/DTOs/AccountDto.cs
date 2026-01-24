using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Modules.DTOs;

public record AccountDto(int Id, string AccountNumber, decimal Balance, DateTimeOffset? FrozenAt)
{
    public AccountDto(BankAccount bankAccount) : this(bankAccount.Id, bankAccount.AccountNumber, bankAccount.Balance, bankAccount.FrozenAt)
    {
        
    }
}