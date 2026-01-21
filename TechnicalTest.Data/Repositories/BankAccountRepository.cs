using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Repositories;

public class BankAccountRepository(ApplicationContext context) : BaseRepository<BankAccount>(context)
{
    protected override DbSet<BankAccount> GetDbSet()
    {
        return context.BankAccounts;
    }
}