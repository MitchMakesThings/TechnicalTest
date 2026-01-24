using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface ICustomerAdminModule
{
    Task<CustomerModificationResult> Create(CustomerCreationDto details);
    IAsyncEnumerable<CustomerDto> GetAll();
    Task<bool> Exists(int customerId);
    Task<CustomerModificationResult> Delete(int customerId);
}

public class CustomerAdminModule(
    IRepository<Customer> customerRepository, 
    ICustomerModule customerModule, 
    IBankAccountAdminModule bankAccountAdminModule,
    IDatabaseTransactionHelper transactionHelper) : ICustomerAdminModule
{
    public async Task<CustomerModificationResult> Create(CustomerCreationDto details)
    {
        var errors = new List<CustomerModificationError>();
        if (String.IsNullOrWhiteSpace(details.Name))
        {
            errors.Add(CustomerModificationError.InvalidName);
        }

        if (details.DateOfBirth is null)
        {
            errors.Add(CustomerModificationError.InvalidDateOfBirth);
        }

        if (details.DailyLimit < 0)
        {
            errors.Add(CustomerModificationError.InvalidDailyLimit);
        }

        if (details.InitialBalance < 0)
        {
            errors.Add(CustomerModificationError.InvalidInitialBalance);
        }

        if (errors.Any())
        {
            return new CustomerModificationResult(false, errors.ToArray());
        }
        
        // Wrap in transaction, as the initial bank account needs to be created for any money to flow into the system
        await using var transaction = await transactionHelper.BeginTransaction();
        
        var customer = new Customer()
        {
            Name = details.Name ?? throw new InvalidOperationException(),
            DateOfBirth = details.DateOfBirth ?? throw new InvalidOperationException(),
            DailyLimit = details.DailyLimit ?? 1_000 // Some sort of company-defined sensible default
        };
        
        var created = await customerRepository.AddAsync(customer);
        await customerRepository.SaveChangesAsync();
        
        // Create the initial bank account, with the specified balance. This is the only way new money flows into the system, as the prototype doesn't have cross-customer/bank transactions!
        var result = await bankAccountAdminModule.Create(created.Id, details.InitialBalance);
        if (!result.Success)
        {
            throw new Exception(
                "Couldn't create initial bank account. This is a game-over scenario! Explode and spam logs");
        }

        await transaction.CommitAsync();

        // Re-fetch so retrieving customer details always goes through a single code-path (Also ensures any EF behind the scenes magic gets picked up)
        var createdCustomer = await customerModule.Get(created.Id);
        if (createdCustomer is null)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.NotFound]);
        }

        return new CustomerModificationResult(true, null, createdCustomer);
    }

    public IAsyncEnumerable<CustomerDto> GetAll()
    {
        return customerRepository
            .GetQueryable()
            .Include(c => c.BankAccounts)
            .AsNoTrackingWithIdentityResolution()
            .Select(c => new CustomerDto(c))
            .AsAsyncEnumerable();
    }

    public Task<bool> Exists(int customerId)
    {
        return customerRepository
            .GetQueryable()
            .AnyAsync(c => c.Id == customerId);
    }

    public async Task<CustomerModificationResult> Delete(int customerId)
    {
        var customer = await customerRepository.GetQueryable().SingleOrDefaultAsync(c => c.Id == customerId);
        if (customer is null)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.NotFound]);
        }
        
        // In the world of finance there is no deleted, only hidden. We'll soft-delete the customer, and our repositories will stop returning them.
        // Future development could add ISoftDeletedRepositories that also return soft-deleted records, for investigations/audits etc
        customer.DeletedAt = DateTime.UtcNow;
        await customerRepository.SaveChangesAsync();

        return new CustomerModificationResult(true);
    }
}