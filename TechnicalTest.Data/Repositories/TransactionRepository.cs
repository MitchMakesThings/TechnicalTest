using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Repositories;

public class TransactionRepository(ApplicationContext context) : BaseRepository<Transaction>(context)
{
    protected override DbSet<Transaction> GetDbSet()
    {
        return context.Transactions;
    }
}