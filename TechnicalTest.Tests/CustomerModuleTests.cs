using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;
using TechnicalTest.Data;

namespace TechnicalTest.Tests;

public class CustomerModuleTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly ICustomerModule _customerModule;
    private readonly SqliteConnection _connection;

    public CustomerModuleTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationContext(options);
        _context.Database.EnsureCreated();

        var customerRepository = new CustomerRepository(_context);
        _customerModule = new CustomerModule(customerRepository);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Get_ShouldReturnCustomer_WhenCustomerExists()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _customerModule.Get(customer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result.Id);
        Assert.Equal("John Doe", result.Name);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenCustomerDoesNotExist()
    {
        // Act
        var result = await _customerModule.Get(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Update_ShouldUpdateFields_WhenInputIsValid()
    {
        // Arrange
        var customer = new Customer { Name = "Original Name", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var updateDto = new CustomerModificationDto("New Name", new DateOnly(1991, 2, 2), 2000);

        // Act
        var result = await _customerModule.Update(customer.Id, updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Customer);
        Assert.Equal("New Name", result.Customer.Name);
        Assert.Equal(new DateOnly(1991, 2, 2), result.Customer.DateOfBirth);
        Assert.Equal(2000, result.Customer.DailyLimit);

        var customerInDb = await _context.Customers.FindAsync(customer.Id);
        Assert.Equal("New Name", customerInDb!.Name);
        Assert.Equal(new DateOnly(1991, 2, 2), customerInDb.DateOfBirth);
        Assert.Equal(2000, customerInDb.DailyLimit);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        var updateDto = new CustomerModificationDto("Name", null, 1000);

        // Act
        var result = await _customerModule.Update(999, updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(CustomerModificationError.NotFound, result.Errors!);
    }

    [Fact]
    public async Task Update_ShouldReturnInvalidDailyLimit_WhenDailyLimitIsNegative()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var updateDto = new CustomerModificationDto(null, null, -1);

        // Act
        var result = await _customerModule.Update(customer.Id, updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(CustomerModificationError.InvalidDailyLimit, result.Errors!);
    }

    [Fact]
    public async Task Update_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var originalDob = new DateOnly(1990, 1, 1);
        var customer = new Customer { Name = "Original Name", DailyLimit = 1000, DateOfBirth = originalDob };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Only update Name
        var updateNameDto = new CustomerModificationDto("Updated Name", null, null);

        // Act
        var result = await _customerModule.Update(customer.Id, updateNameDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated Name", result.Customer!.Name);
        Assert.Equal(originalDob, result.Customer.DateOfBirth);
        Assert.Equal(1000, result.Customer.DailyLimit);

        // Only update DailyLimit
        var updateLimitDto = new CustomerModificationDto(null, null, 5000);
        result = await _customerModule.Update(customer.Id, updateLimitDto);

        Assert.True(result.Success);
        Assert.Equal("Updated Name", result.Customer!.Name);
        Assert.Equal(5000, result.Customer.DailyLimit);
    }

    [Fact]
    public async Task Update_ShouldReturnInvalidDateOfBirth_WhenDateOfBirthIsTooLongAgo()
    {
        // Arrange
        var customer = new Customer { Name = "John Doe", DailyLimit = 1000, DateOfBirth = new DateOnly(1990, 1, 1) };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // 121 years ago
        var invalidDob = DateOnly.FromDateTime(DateTime.Now.AddYears(-121));
        var updateDto = new CustomerModificationDto(null, invalidDob, null);

        // Act
        var result = await _customerModule.Update(customer.Id, updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(CustomerModificationError.InvalidDateOfBirth, result.Errors!);
    }
}
