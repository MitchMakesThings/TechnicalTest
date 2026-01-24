using System.Text.Json.Serialization;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Modules.DTOs;

[method: JsonConstructor]
public record AccountDto(int Id, string AccountNumber, decimal Balance, bool Frozen)
{
    public AccountDto(BankAccount bankAccount) : this(bankAccount.Id, bankAccount.AccountNumber, bankAccount.Balance, bankAccount.FrozenAt.HasValue)
    {
        
    }
}