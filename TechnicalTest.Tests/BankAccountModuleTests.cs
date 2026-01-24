using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;
using TechnicalTest.Data;

namespace TechnicalTest.Tests;

public class BankAccountModuleTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly IBankAccountModule _bankAccountModule;
    private readonly SqliteConnection _connection;

    public BankAccountModuleTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationContext(options);
        _context.Database.EnsureCreated();

        var bankAccountRepository = new BankAccountRepository(_context);
        _bankAccountModule = new BankAccountModule(bankAccountRepository);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Get_ShouldReturnAccounts_WhenAccountsExist()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var account1 = new BankAccount { CustomerId = customer.Id, AccountNumber = "12345678901", Balance = 100 };
        var account2 = new BankAccount { CustomerId = customer.Id, AccountNumber = "98765432109", Balance = 200 };
        _context.BankAccounts.AddRange(account1, account2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bankAccountModule.Get(customer.Id);

        // Assert
        var accountDtos = result.ToList();
        Assert.Equal(2, accountDtos.Count);
        Assert.Contains(accountDtos, a => a.AccountNumber == "12345678901");
        Assert.Contains(accountDtos, a => a.AccountNumber == "98765432109");
    }

    [Fact]
    public async Task Create_ShouldCreateNewAccount()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bankAccountModule.Create(customer.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Account);
        Assert.Equal(11, result.Account.AccountNumber.Length);

        var accountInDb = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == result.Account.Id);
        Assert.NotNull(accountInDb);
        Assert.Equal(customer.Id, accountInDb.CustomerId);
    }

    [Fact]
    public async Task Update_ShouldUpdateFrozenStatus()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var account = new BankAccount { CustomerId = customer.Id, AccountNumber = "12345678901", Balance = 0 };
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();

        var updateDto = new AccountDto(account.Id, account.AccountNumber, account.Balance, true);

        // Act - Freeze
        var result = await _bankAccountModule.Update(customer.Id, account.Id, updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Account!.Frozen);
        
        var accountInDb = await _context.BankAccounts.FindAsync(account.Id);
        Assert.NotNull(accountInDb!.FrozenAt);

        // Act - Unfreeze
        updateDto = updateDto with { Frozen = false };
        result = await _bankAccountModule.Update(customer.Id, account.Id, updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Account!.Frozen);
        
        await _context.Entry(accountInDb).ReloadAsync();
        Assert.Null(accountInDb.FrozenAt);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteAccount_WhenConditionsAreMet()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var account = new BankAccount { CustomerId = customer.Id, AccountNumber = "12345678901", Balance = 0 };
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bankAccountModule.Delete(customer.Id, account.Id);

        // Assert
        Assert.True(result.Success);
        
        var accountInDb = await _context.BankAccounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == account.Id);
        Assert.NotNull(accountInDb!.DeletedAt);
    }

    [Fact]
    public async Task Delete_ShouldReturnError_WhenAccountIsFrozen()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var account = new BankAccount { CustomerId = customer.Id, AccountNumber = "12345678901", Balance = 0, FrozenAt = DateTimeOffset.UtcNow };
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bankAccountModule.Delete(customer.Id, account.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(BankAccountModificationError.AccountFrozen, result.Errors!);
    }

    [Fact]
    public async Task Delete_ShouldReturnError_WhenBalanceIsNotZero()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var account = new BankAccount { CustomerId = customer.Id, AccountNumber = "12345678901", Balance = 100 };
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bankAccountModule.Delete(customer.Id, account.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(BankAccountModificationError.InvalidBalance, result.Errors!);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // Act
        var result = await _bankAccountModule.Delete(1, 999);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(BankAccountModificationError.NotFound, result.Errors!);
    }
}
