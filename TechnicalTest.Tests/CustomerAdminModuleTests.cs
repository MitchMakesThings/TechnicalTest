using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Moq;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;
using TechnicalTest.Data;

namespace TechnicalTest.Tests;

public class CustomerAdminModuleTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly ICustomerAdminModule _customerAdminModule;
    private readonly SqliteConnection _connection;

    public CustomerAdminModuleTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationContext(options);
        _context.Database.EnsureCreated();

        var customerRepository = new CustomerRepository(_context);
        var bankAccountRepository = new BankAccountRepository(_context);
        var customerModule = new CustomerModule(customerRepository);
        var bankAccountModule = new BankAccountModule(bankAccountRepository);
        var bankAccountAdminModule = new BankAccountAdminModule(bankAccountModule, bankAccountRepository);
        var transactionHelper = new DatabaseTransactionHelper(_context);

        _customerAdminModule = new CustomerAdminModule(
            customerRepository,
            customerModule,
            bankAccountAdminModule,
            transactionHelper);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Create_ShouldReturnErrors_WhenInputIsInvalid()
    {
        // Arrange
        var details = new CustomerCreationDto(null!, null, -1, -1);

        // Act
        var result = await _customerAdminModule.Create(details);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(CustomerModificationError.InvalidName, result.Errors!);
        Assert.Contains(CustomerModificationError.InvalidDateOfBirth, result.Errors!);
        Assert.Contains(CustomerModificationError.InvalidDailyLimit, result.Errors!);
        Assert.Contains(CustomerModificationError.InvalidInitialBalance, result.Errors!);
    }

    [Fact]
    public async Task Create_ShouldCreateCustomerAndAccount_WhenInputIsValid()
    {
        // Arrange
        var details = new CustomerCreationDto("John Doe", new DateOnly(1990, 1, 1), 1000, 500);

        // Act
        var result = await _customerAdminModule.Create(details);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Customer);
        Assert.Equal("John Doe", result.Customer.Name);
        
        var customerInDb = await _context.Customers
            .Include(c => c.BankAccounts)
            .FirstOrDefaultAsync(c => c.Id == result.Customer.Id);
        
        Assert.NotNull(customerInDb);
        Assert.Single(customerInDb.BankAccounts);
        Assert.Equal(500, customerInDb.BankAccounts.First().Balance);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteCustomer_WhenCustomerExists()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerAdminModule.Delete(customer.Id);

        // Assert
        Assert.True(result.Success);
        
        var customerInDb = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == customer.Id);
        Assert.NotNull(customerInDb!.DeletedAt);
        
        // BaseRepository's GetQueryable filters out deleted entities
        var exists = await _customerAdminModule.Exists(customer.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        int customerId = 999;

        // Act
        var result = await _customerAdminModule.Delete(customerId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(CustomerModificationError.NotFound, result.Errors!);
    }

    [Fact]
    public async Task Exists_ShouldReturnTrue_WhenCustomerExists()
    {
        // Arrange
        var customer = new Customer { Name = "Test", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerAdminModule.Exists(customer.Id);

        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task Exists_ShouldReturnFalse_WhenCustomerDoesntExist()
    {
        // Act
        var result = await _customerAdminModule.Exists(999999999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllCustomers()
    {
        // Arrange
        _context.Customers.Add(new Customer { Name = "John", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) });
        _context.Customers.Add(new Customer { Name = "Jane", DailyLimit = 1000, DateOfBirth = new DateOnly(1992, 2, 2) });
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerAdminModule.GetAll().ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "John");
        Assert.Contains(result, c => c.Name == "Jane");
    }

    [Fact]
    public async Task Create_ShouldThrow_WhenBankAccountCreationFails()
    {
        // Arrange
        // We use real implementations for most things, but mock IBankAccountAdminModule to simulate failure
        var customerRepository = new CustomerRepository(_context);
        var customerModule = new CustomerModule(customerRepository);
        var bankAccountAdminModuleMock = new Mock<IBankAccountAdminModule>();
        var transactionHelper = new DatabaseTransactionHelper(_context);
        
        var sut = new CustomerAdminModule(
            customerRepository,
            customerModule,
            bankAccountAdminModuleMock.Object,
            transactionHelper);

        bankAccountAdminModuleMock.Setup(x => x.Create(It.IsAny<int>(), It.IsAny<decimal>()))
            .ReturnsAsync(new BankAccountModificationResult(false, null, null));

        var details = new CustomerCreationDto("John Doe", new DateOnly(1990, 1, 1), 1000, 500);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.Create(details));
        
        // Verify transaction rolled back (no customer should be in DB)
        var customers = await _context.Customers.ToListAsync();
        Assert.Empty(customers);
    }
}
