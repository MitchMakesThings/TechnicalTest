using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface ITransactionModule
{
    Task<IEnumerable<TransactionDto>> GetTransactions(int customerId, int bankAccountId);

    Task<TransactionModificationResult> Create(
        int customerId,
        int debitBankAccountId,
        int creditBankAccountId,
        decimal amount,
        string? reference);
}

public class TransactionModule(IRepository<Transaction> transactionRepo, IRepository<BankAccount> accountRepo) : ITransactionModule
{
    public async Task<IEnumerable<TransactionDto>> GetTransactions(int customerId, int bankAccountId)
    {
        var account = await accountRepo.GetQueryable().SingleOrDefaultAsync(a => a.Id == bankAccountId);
        if (account == null || account.CustomerId != customerId)
        {
            throw new KeyNotFoundException("Account not found");
        }

        return await transactionRepo
            .GetQueryable()
            .Include(transaction => transaction.DebitBankAccount)
            .Include(transaction => transaction.CreditBankAccount)
            .Where(t => t.DebitBankAccountId == bankAccountId || t.CreditBankAccountId == bankAccountId)
            .Select(t => new TransactionDto(t))
            .ToListAsync();
    }

    public async Task<TransactionModificationResult> Create(
        int customerId,
        int debitBankAccountId,
        int creditBankAccountId,
        decimal amount,
        string? reference)
    {
        var errors = new List<TransactionModificationError>();
        if (amount <= 0)
        {
            // In this case we can return early without hitting the data layer
            errors.Add(TransactionModificationError.InvalidAmount);
        }

        if (debitBankAccountId == creditBankAccountId)
        {
            errors.Add(TransactionModificationError.InvalidAccounts);
        }

        if (errors.Any())
        {
            return new TransactionModificationResult(false, errors.ToArray());
        }
        
        // ASSUMPTION: A real product should take a lock on the source account at this point, so there is no race condition between validating funds below and writing the transaction.
        // For the tech test, I'm leaving it out of scope as the solution would depend on infrastructure decisions etc (multiple web servers etc) 
        
        var accounts = await accountRepo
            .GetQueryable()
            .Include(a => a.Customer)
            .Where(a => a.Id == debitBankAccountId || a.Id == creditBankAccountId).ToListAsync();
        if (accounts.Count != 2)
        {
            errors.Add(TransactionModificationError.InvalidAccounts);
        }
        
        var debitAccount = accounts.FirstOrDefault(a => a.Id == debitBankAccountId);
        var creditAccount =  accounts.FirstOrDefault(a => a.Id == creditBankAccountId);

        if (debitAccount is null || debitAccount.CustomerId != customerId)
        {
            errors.Add(TransactionModificationError.DebitAccountNotFound);
        } else if (debitAccount.FrozenAt is not null)
        {
            errors.Add(TransactionModificationError.DebitAccountFrozen);
        }

        if (creditAccount is null)
        {
            errors.Add(TransactionModificationError.CreditAccountNotFound);
        } else if (creditAccount.FrozenAt is not null)
        {
            errors.Add(TransactionModificationError.CreditAccountFrozen);
        }
        
        if ((debitAccount?.Balance ?? 0) < amount)
        {
            errors.Add(TransactionModificationError.InsufficientFunds);
        }
        
        if (errors.Any())
        {
            // Again, return if we've hit an error case to save hitting the data layer again
            return new TransactionModificationResult(false, errors.ToArray());
        }
        
        // ASSUMPTION: Caching is out of scope, but this would be a great time to pull from a customers daily-transaction-total cache.
        // We would then update it at the end of this method when the new transaction is written.

        var dailyTransactionTotal = await SumDailyTransactions(debitAccount!.CustomerId);
        if (debitAccount.Customer!.DailyLimit < dailyTransactionTotal + amount)
        {
            return new TransactionModificationResult(false, [TransactionModificationError.DailyLimitReached]);
        }

        // Insert the transaction
        var newTransaction = await transactionRepo.AddAsync(new Transaction()
        {
            DebitBankAccountId = debitBankAccountId,
            CreditBankAccountId = creditBankAccountId,
            Amount = amount,
            Reference = reference
        });

        // And decrement the account balance!
        debitAccount.Balance -= amount;
        
        // Note that because the repo is calling the context SaveChangesAsync, we only need to save one repo for it to also save the account
        await transactionRepo.SaveChangesAsync();

        return new TransactionModificationResult(true, Transaction: new TransactionDto(newTransaction));
    }

    private async Task<Decimal> SumDailyTransactions(int customerId)
    {
        return await accountRepo
            .GetQueryable()
            .Where(a => a.CustomerId == customerId)
            .SelectMany(a => a.DebitTransactions)
            .SumAsync(t => t.Amount);
    }
}


public enum TransactionModificationError
{
    DebitAccountNotFound,
    CreditAccountNotFound,
    InvalidAccounts,
    InsufficientFunds,
    InvalidAmount,
    DailyLimitReached,
    DebitAccountFrozen,
    CreditAccountFrozen,
};
public record TransactionModificationResult(bool Success, TransactionModificationError[]? Errors = null, TransactionDto? Transaction = null);