using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface ICustomerModule
{
    Task<CustomerDto?> Get(int customerId);
    Task<CustomerDto> Update(int id, CustomerModification newDetails);
    Task Delete(int customerId);
}

public interface ICustomerAdminModule
{
    Task<CustomerDto> Create(CustomerModification details);
    IAsyncEnumerable<CustomerDto> GetAll();
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

    public async Task<CustomerDto> Update(int id, CustomerModification newDetails)
    {
        var existing = await _customerRepository
            .GetQueryable()
            .SingleOrDefaultAsync(c => c.Id == id);

        if (existing == null)
        {
            throw new KeyNotFoundException($"Customer with id {id} not found");
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
        return new CustomerDto(existing);
    }

    public async Task<CustomerDto> Create(CustomerModification details)
    {
        var customer = new Customer()
        {
            Name = details.Name ?? throw new InvalidOperationException(),
            DateOfBirth = details.DateOfBirth ?? throw new InvalidOperationException(),
            DailyLimit = details.DailyLimit ?? 1_000 // Some sort of company-defined sensible default
        };
        
        var created = await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        // Re-fetch so retrieving customer details always goes through a single codepath (Also ensures any EF behind the scenes magic gets picked up)
        return await Get(created.Id) ??  throw new InvalidOperationException("Customer creation ended with customer not found");
    }

    public async Task Delete(int customerId)
    {
        var customer = await _customerRepository.GetQueryable().SingleOrDefaultAsync(c => c.Id == customerId);
        if (customer is null) throw new KeyNotFoundException($"Customer with id {customerId} not found");
        
        // In the world of finance there is no deleted, only hidden. We'll soft-delete the customer, and our repositories will stop returning them.
        // Future development could add ISoftDeletedRepositories that also return soft-deleted records, for investigations/audits etc
        customer.DeletedAt = DateTime.UtcNow;
        await _customerRepository.SaveChangesAsync();
    }
}

public record CustomerModification(string? Name, DateOnly? DateOfBirth, decimal? DailyLimit);

public record CustomerDto(string Name, DateOnly DateOfBirth, decimal DailyLimit)
{
    public CustomerDto(Customer Customer) : this(Customer.Name, Customer.DateOfBirth, Customer.DailyLimit)
    {
    }

} // TODO BankAccountDto[]
