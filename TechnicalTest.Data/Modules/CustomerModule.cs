using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface ICustomerModule
{
    Task<CustomerDto?> Get(int customerId);
    Task<CustomerModificationResult> Update(int id, CustomerModification newDetails);
    Task<CustomerModificationResult> Delete(int customerId);
}

public interface ICustomerAdminModule
{
    Task<CustomerModificationResult> Create(CustomerModification details);
    IAsyncEnumerable<CustomerDto> GetAll();
    Task<bool> Exists(int customerId);
}

public class CustomerModule(IRepository<Customer> customerRepository) : ICustomerModule, ICustomerAdminModule
{
    private readonly IRepository<Customer> _customerRepository = customerRepository;

    public IAsyncEnumerable<CustomerDto> GetAll()
    {
        return _customerRepository
            .GetQueryable()
            .Include(c => c.BankAccounts)
            .AsNoTrackingWithIdentityResolution()
            .Select(c => new CustomerDto(c))
            .AsAsyncEnumerable();
    }

    public Task<bool> Exists(int customerId)
    {
        return _customerRepository
            .GetQueryable()
            .AnyAsync(c => c.Id == customerId);
    }

    public async Task<CustomerDto?> Get(int customerId)
    {
        var customer = await _customerRepository
            .GetQueryable()
            .Include(c => c.BankAccounts)
            .AsNoTrackingWithIdentityResolution()
            .SingleOrDefaultAsync(c => c.Id == customerId);

        if (customer is null) return null;

        return new CustomerDto(customer);
    }

    public async Task<CustomerModificationResult> Update(int id, CustomerModification newDetails)
    {
        var existing = await _customerRepository
            .GetQueryable()
            .SingleOrDefaultAsync(c => c.Id == id);

        if (existing == null)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.NotFound]);
        }

        // Copy across the fields a customer should be able to update about themselves
        existing.Name = newDetails.Name ?? existing.Name;
        existing.DailyLimit = newDetails.DailyLimit ?? existing.DailyLimit;
        if (newDetails.DateOfBirth != null)
        {
            existing.DateOfBirth = newDetails.DateOfBirth ?? existing.DateOfBirth;
            // TODO trigger the klaxon and get an auditor looking at this person ASAP
        }

        await _customerRepository.SaveChangesAsync();
        return new CustomerModificationResult(true, null, new CustomerDto(existing));
    }

    public async Task<CustomerModificationResult> Create(CustomerModification details)
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

        if (errors.Any())
        {
            return new CustomerModificationResult(false, errors.ToArray());
        }
        
        var customer = new Customer()
        {
            Name = details.Name ?? throw new InvalidOperationException(),
            DateOfBirth = details.DateOfBirth ?? throw new InvalidOperationException(),
            DailyLimit = details.DailyLimit ?? 1_000 // Some sort of company-defined sensible default
        };
        
        var created = await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        // Re-fetch so retrieving customer details always goes through a single codepath (Also ensures any EF behind the scenes magic gets picked up)
        var createdCustomer = await Get(created.Id);
        if (createdCustomer is null)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.NotFound]);
        }

        return new CustomerModificationResult(true, null, createdCustomer);
    }

    public async Task<CustomerModificationResult> Delete(int customerId)
    {
        var customer = await _customerRepository.GetQueryable().SingleOrDefaultAsync(c => c.Id == customerId);
        if (customer is null)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.NotFound]);
        }
        
        // In the world of finance there is no deleted, only hidden. We'll soft-delete the customer, and our repositories will stop returning them.
        // Future development could add ISoftDeletedRepositories that also return soft-deleted records, for investigations/audits etc
        customer.DeletedAt = DateTime.UtcNow;
        await _customerRepository.SaveChangesAsync();

        return new CustomerModificationResult(true);
    }
}

public enum CustomerModificationError
{
    NotFound,
    InvalidName,
    InvalidDateOfBirth,

};
public record CustomerModificationResult(bool Success, CustomerModificationError[]? Errors = null, CustomerDto? Result = null);

public record CustomerModification(string? Name, DateOnly? DateOfBirth, decimal? DailyLimit);

public record CustomerDto(int Id, string Name, DateOnly DateOfBirth, decimal DailyLimit)
{
    public CustomerDto(Customer customer) : this(customer.Id, customer.Name, customer.DateOfBirth, customer.DailyLimit)
    {
    }

} // TODO BankAccountDto[]
