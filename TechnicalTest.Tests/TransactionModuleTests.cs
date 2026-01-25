using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;
using TechnicalTest.Data;

namespace TechnicalTest.Tests;

public class TransactionModuleTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly ITransactionModule _transactionModule;
    private readonly SqliteConnection _connection;

    public TransactionModuleTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationContext(options);
        _context.Database.EnsureCreated();

        var transactionRepository = new TransactionRepository(_context);
        var bankAccountRepository = new BankAccountRepository(_context);
        _transactionModule = new TransactionModule(transactionRepository, bankAccountRepository);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<Customer> CreateCustomer(string name = "John Doe", decimal dailyLimit = 1000)
    {
        var customer = new Customer { Name = name, DailyLimit = dailyLimit, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    private async Task<BankAccount> CreateAccount(int customerId, string accountNumber, decimal balance = 0, DateTimeOffset? frozenAt = null)
    {
        var account = new BankAccount { CustomerId = customerId, AccountNumber = accountNumber, Balance = balance, FrozenAt = frozenAt };
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    [Fact]
    public async Task GetTransactions_ShouldReturnTransactions_WhenTheyExist()
    {
        // Arrange
        var customer = await CreateCustomer();
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);
        var account3 = await CreateAccount(customer.Id, "33333333333", 1000);

        var t1 = new Transaction { DebitBankAccountId = account1.Id, CreditBankAccountId = account2.Id, Amount = 100, Reference = "T1" };
        var t2 = new Transaction { DebitBankAccountId = account3.Id, CreditBankAccountId = account1.Id, Amount = 200, Reference = "T2" };
        var t3 = new Transaction { DebitBankAccountId = account2.Id, CreditBankAccountId = account3.Id, Amount = 300, Reference = "T3" };

        _context.Transactions.AddRange(t1, t2, t3);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _transactionModule.GetTransactions(customer.Id, account1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Reference == "T1");
        Assert.Contains(result, t => t.Reference == "T2");
    }

    [Fact]
    public async Task GetTransactions_ShouldReturnEmpty_WhenAccountDoesNotBelongToCustomer()
    {
        // Arrange
        var customer1 = await CreateCustomer("Customer 1");
        var customer2 = await CreateCustomer("Customer 2");
        var account1 = await CreateAccount(customer1.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer2.Id, "22222222222", 1000);
        
        var t1 = new Transaction { DebitBankAccountId = account1.Id, CreditBankAccountId = account2.Id, Amount = 100, Reference = "T1" };
        _context.Transactions.Add(t1);
        await _context.SaveChangesAsync();

        // Act - Customer 2 tries to get transactions for Customer 1's account
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _transactionModule.GetTransactions(customer2.Id, account1.Id));

        // Assert
        Assert.Equal("Account not found", exception.Message);
    }

    [Fact]
    public async Task Create_ShouldReturnInvalidAmount_WhenAmountIsZeroOrNegative()
    {
        // Act
        var resultZero = await _transactionModule.Create(1, 1, 2, 0, "Test");
        var resultNegative = await _transactionModule.Create(1, 1, 2, -10, "Test");

        // Assert
        Assert.False(resultZero.Success);
        Assert.Contains(TransactionModificationError.InvalidAmount, resultZero.Errors!);
        Assert.False(resultNegative.Success);
        Assert.Contains(TransactionModificationError.InvalidAmount, resultNegative.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnInvalidAccounts_WhenAccountsAreSame()
    {
        // Act
        var result = await _transactionModule.Create(1, 1, 1, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.InvalidAccounts, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenAccountsDoNotExist()
    {
        // Act
        var result = await _transactionModule.Create(1, 998, 999, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.InvalidAccounts, result.Errors!); // TransactionModule adds InvalidAccounts if count != 2
        Assert.Contains(TransactionModificationError.DebitAccountNotFound, result.Errors!);
        Assert.Contains(TransactionModificationError.CreditAccountNotFound, result.Errors!);
    }


    [Fact]
    public async Task Create_ShouldReturnInsufficientFunds_WhenBalanceIsTooLow()
    {
        // Arrange
        var customer = await CreateCustomer();
        var account1 = await CreateAccount(customer.Id, "11111111111", 50);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);

        // Act
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.InsufficientFunds, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnDailyLimitReached_WhenLimitIsExceeded()
    {
        // Arrange
        var customer = await CreateCustomer(dailyLimit: 100);
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);

        // Act
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 150, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.DailyLimitReached, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnDailyLimitReached_WhenExistingTransactionsPushOverLimit()
    {
        // Arrange
        var customer = await CreateCustomer(dailyLimit: 200);
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);
        
        // Existing transaction of 150
        _context.Transactions.Add(new Transaction { DebitBankAccountId = account1.Id, CreditBankAccountId = account2.Id, Amount = 150 });
        await _context.SaveChangesAsync();

        // Act - try to send another 100 (total 250 > 200)
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.DailyLimitReached, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenExistingTransactionsAreMoreThan24HoursOld()
    {
        // Arrange
        var customer = await CreateCustomer(dailyLimit: 200);
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);

        // Existing transaction of 500 from 25 hours ago
        _context.Transactions.Add(new Transaction
        {
            DebitBankAccountId = account1.Id,
            CreditBankAccountId = account2.Id,
            Amount = 500,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-25)
        });
        await _context.SaveChangesAsync();

        // Act - try to send 100 (limit is 200, total old is 500, but they should be ignored)
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 100, "Test");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task Create_ShouldReturnFrozen_WhenDebitAccountIsFrozen()
    {
        // Arrange
        var customer = await CreateCustomer();
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000, frozenAt: DateTimeOffset.UtcNow);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);

        // Act
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.DebitAccountFrozen, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnFrozen_WhenCreditAccountIsFrozen()
    {
        // Arrange
        var customer = await CreateCustomer();
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000, frozenAt: DateTimeOffset.UtcNow);

        // Act
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.CreditAccountFrozen, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenDebitAccountDoesNotExist()
    {
        // Arrange
        var customer = await CreateCustomer();
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);

        // Act
        var result = await _transactionModule.Create(customer.Id, 999, account2.Id, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.DebitAccountNotFound, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenCreditAccountDoesNotExist()
    {
        // Arrange
        var customer = await CreateCustomer();
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);

        // Act
        var result = await _transactionModule.Create(customer.Id, account1.Id, 999, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.CreditAccountNotFound, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenDebitAccountDoesNotBelongToCustomer()
    {
        // Arrange
        var customer1 = await CreateCustomer("Customer 1");
        var customer2 = await CreateCustomer("Customer 2");
        var accountFromCustomer2 = await CreateAccount(customer2.Id, "22222222222", 1000);
        var someOtherAccount = await CreateAccount(customer1.Id, "11111111111", 1000);

        // Act - Customer 1 tries to use Customer 2's account as debit
        var result = await _transactionModule.Create(customer1.Id, accountFromCustomer2.Id, someOtherAccount.Id, 100, "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(TransactionModificationError.DebitAccountNotFound, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenValid()
    {
        // Arrange
        var customer = await CreateCustomer(dailyLimit: 1000);
        var account1 = await CreateAccount(customer.Id, "11111111111", 1000);
        var account2 = await CreateAccount(customer.Id, "22222222222", 1000);

        // Act
        var result = await _transactionModule.Create(customer.Id, account1.Id, account2.Id, 100, "Payment");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Transaction);
        Assert.Equal(100, result.Transaction.Amount);
        Assert.Equal("Payment", result.Transaction.Reference);
        Assert.Equal("11111111111", result.Transaction.DebitAccountNumber);
        Assert.Equal("22222222222", result.Transaction.CreditAccountNumber);

        var transactionInDb = await _context.Transactions.FirstOrDefaultAsync();
        Assert.NotNull(transactionInDb);
        Assert.Equal(account1.Id, transactionInDb.DebitBankAccountId);
        Assert.Equal(account2.Id, transactionInDb.CreditBankAccountId);
        Assert.Equal(100, transactionInDb.Amount);
    }

    [Fact]
    public async Task Create_ShouldDecrementDebitAccountBalance_WhenSucceeds()
    {
        // Arrange
        var customer = await CreateCustomer();
        var initialBalance = 1000m;
        var transactionAmount = 100m;
        var debitAccount = await CreateAccount(customer.Id, "11111111111", initialBalance);
        var creditAccount = await CreateAccount(customer.Id, "22222222222", 500m);

        // Act
        var result = await _transactionModule.Create(customer.Id, debitAccount.Id, creditAccount.Id, transactionAmount, "Decrease Balance Test");

        // Assert
        Assert.True(result.Success);

        // Reload debit account from DB to check balance
        var updatedDebitAccount = await _context.BankAccounts.FindAsync(debitAccount.Id);
        Assert.NotNull(updatedDebitAccount);
        Assert.Equal(initialBalance - transactionAmount, updatedDebitAccount.Balance);
    }
}
