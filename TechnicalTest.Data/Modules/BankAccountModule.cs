using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface IBankAccountModule
{
    Task<IEnumerable<AccountDto>> Get(int customerId);
    Task<BankAccountModificationResult> Create(int customerId);
    Task<BankAccountModificationResult> Freeze(int customerId, int accountId);
    Task<BankAccountModificationResult> Delete(int customerId, int accountId);
}

public class BankAccountModule(IRepository<BankAccount> bankAccountRepository) : IBankAccountModule
{
    public async Task<IEnumerable<AccountDto>> Get(int customerId)
    {
        return await bankAccountRepository
            .GetQueryable()
            .Where(a => a.CustomerId == customerId)
            .Select(account => new AccountDto(account))
            .ToListAsync();
    }

    public async Task<BankAccountModificationResult> Create(int customerId)
    {
        // ASSUMPTION: Requesting an account just generates a new one with a unique code.
        // Account numbers are very domain-specific, so in a real example we'd have some sort of generation class.
        // Eg, NZ accounts are prefixed with bank-specific fixed values, then a customer-specific number, then a 3 digit account number.
        // In that case the account number could easily be generated in SQL
        
        // For the tech test we'll just generate a pseudo-random string. If it happens to collide we'll try another one.
        // This is obviously bad. To make things worse, we're just trying a few times in a loop letting the SQL constraint reject us.
        var accountNumber = RandomNumberGenerator.GetString("0123456789", 11);
        var attemptsRemaining = 5;
        while (attemptsRemaining > 0)
        {
            try
            {
                var account = await CreateNewAccount(customerId, accountNumber);
                return new BankAccountModificationResult(true, null, new AccountDto(account));
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("SQLite Error 19: 'UNIQUE constraint failed: BankAccounts.AccountNumber'.") == true)
                {
                    attemptsRemaining--;
                }
                else
                {
                    throw;
                }
            }
        }

        throw new Exception("The world is falling and we can't generate new accounts");
    }

    private async Task<BankAccount> CreateNewAccount(int customerId, string accountNumber)
    {
        var account = new BankAccount()
        {
            CustomerId = customerId,
            AccountNumber = accountNumber,
        };
        await bankAccountRepository.AddAsync(account);
        await bankAccountRepository.SaveChangesAsync();

        return account;
    }

    public Task<BankAccountModificationResult> Update(int customerId, int accountId)
    {
        // TODO - admin-level endpoint to support updating the balance?
        throw new NotImplementedException();
    }

    public async Task<BankAccountModificationResult> Freeze(int customerId, int accountId)
    {
        var account = await bankAccountRepository.GetQueryable().SingleOrDefaultAsync(a => a.CustomerId == customerId && a.Id == accountId);
        if (account == null)
        {
            return new BankAccountModificationResult(false, [BankAccountModificationError.NotFound]);
        }

        if (account.FrozenAt.HasValue)
        {
            // Don't update the frozen date, just return the existing frozen account
            return new BankAccountModificationResult(true, Account: new AccountDto(account));
        }
        account.FrozenAt = DateTime.UtcNow;
        await bankAccountRepository.SaveChangesAsync();
        
        return new BankAccountModificationResult(true, Account: new AccountDto(account));
    }

    public async Task<BankAccountModificationResult> Delete(int customerId, int accountId)
    {
        var account = await bankAccountRepository.GetQueryable().SingleOrDefaultAsync(a => a.CustomerId == customerId && a.Id == accountId);
        if (account == null)
        {
            return new BankAccountModificationResult(false, [BankAccountModificationError.NotFound]);
        }

        if (account.FrozenAt.HasValue)
        {
            // ASSUMPTION: Frozen accounts are immutable in all regards, so shouldn't be deleted
            return new BankAccountModificationResult(false, [BankAccountModificationError.AccountFrozen]);
        }

        if (account.Balance != 0)
        {
            return new BankAccountModificationResult(false, [BankAccountModificationError.InvalidBalance]);
        }
        
        account.DeletedAt = DateTime.UtcNow;
        await bankAccountRepository.SaveChangesAsync();
        
        return new BankAccountModificationResult(true, Account: new AccountDto(account));
    }
}

public enum BankAccountModificationError
{
    NotFound,
    AccountFrozen,
    InvalidBalance,
    NoAccountsAvailable

};
public record BankAccountModificationResult(bool Success, BankAccountModificationError[]? Errors = null, AccountDto? Account = null);