using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Repositories;

public class CustomerRepository(ApplicationContext context) : BaseRepository<Customer>(context)
{
    protected override DbSet<Customer> GetDbSet()
    {
        return context.Customers;
    }
}