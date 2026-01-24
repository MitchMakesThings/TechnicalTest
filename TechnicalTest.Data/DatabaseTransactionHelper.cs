using Microsoft.EntityFrameworkCore.Storage;

namespace TechnicalTest.Data;

public interface IDatabaseTransactionHelper
{
    Task<IDbContextTransaction> BeginTransaction();
}

public class DatabaseTransactionHelper(ApplicationContext context) : IDatabaseTransactionHelper
{
    public Task<IDbContextTransaction> BeginTransaction()
    {
        return context.Database.BeginTransactionAsync();
    }
}