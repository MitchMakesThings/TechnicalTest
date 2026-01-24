using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules.DTOs;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data.Modules;

public interface ICustomerModule
{
    Task<CustomerDto?> Get(int customerId);
    Task<CustomerModificationResult> Update(int id, CustomerModificationDto newDetails);
}

public class CustomerModule(IRepository<Customer> customerRepository) : ICustomerModule
{
    public async Task<CustomerDto?> Get(int customerId)
    {
        var customer = await customerRepository
            .GetQueryable()
            .Include(c => c.BankAccounts)
            .AsNoTrackingWithIdentityResolution()
            .SingleOrDefaultAsync(c => c.Id == customerId);

        if (customer is null) return null;

        return new CustomerDto(customer);
    }

    public async Task<CustomerModificationResult> Update(int id, CustomerModificationDto newDetails)
    {
        var existing = await customerRepository
            .GetQueryable()
            .SingleOrDefaultAsync(c => c.Id == id);

        if (existing == null)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.NotFound]);
        }

        if (newDetails.DailyLimit < 0)
        {
            return new CustomerModificationResult(false, [CustomerModificationError.InvalidDailyLimit]);
        }

        // Copy across the fields a customer should be able to update about themselves
        existing.Name = newDetails.Name ?? existing.Name; // TODO raise internal alert about name changes too. No secret identities here!
        existing.DailyLimit = newDetails.DailyLimit ?? existing.DailyLimit;
        if (newDetails.DateOfBirth != null)
        {
            existing.DateOfBirth = newDetails.DateOfBirth ?? existing.DateOfBirth;
            // TODO trigger the klaxon and get an auditor looking at this person ASAP
        }

        await customerRepository.SaveChangesAsync();
        return new CustomerModificationResult(true, null, new CustomerDto(existing));
    }
}

public enum CustomerModificationError
{
    NotFound,
    InvalidName,
    InvalidDateOfBirth,
    InvalidDailyLimit,

};
public record CustomerModificationResult(bool Success, CustomerModificationError[]? Errors = null, CustomerDto? Customer = null);