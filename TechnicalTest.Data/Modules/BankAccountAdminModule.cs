using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface IBankAccountAdminModule
{
    Task<BankAccountModificationResult> Create(int customerId, decimal initialBalance);
}

public class BankAccountAdminModule(IBankAccountModule bankAccountModule, IRepository<BankAccount> bankAccountRepository) : IBankAccountAdminModule
{
    public async Task<BankAccountModificationResult> Create(int customerId, decimal initialBalance)
    {
        var result = await bankAccountModule.Create(customerId);
        if (!result.Success) return result;
        
        // Add an initial starting balance. Pretend the person handed over some cash when they opened their account.
        // This is the only way for new money to enter the system in the tech test prototype!
        var account = await bankAccountRepository
            .GetQueryable()
            .SingleAsync(a => a.CustomerId == customerId && a.Id == result.Account!.Id);

        account.Balance = initialBalance;
        await bankAccountRepository.SaveChangesAsync();

        return new BankAccountModificationResult(true, null, new AccountDto(account));
    }
}