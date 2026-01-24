using Microsoft.Extensions.DependencyInjection;
using TechnicalTest.Data.Models;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Repositories;

namespace TechnicalTest.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTechnicalTestDataServices(this IServiceCollection services)
    {
        services
            .AddScoped<IRepository<BankAccount>, BankAccountRepository>()
            .AddScoped<IRepository<Customer>, CustomerRepository>()
            .AddScoped<IRepository<Transaction>, TransactionRepository>();
        
        services.AddScoped<IDatabaseTransactionHelper, DatabaseTransactionHelper>();
        
        services.AddScoped<ICustomerModule, CustomerModule>()
            .AddScoped<ICustomerAdminModule, CustomerAdminModule>()
            .AddScoped<IBankAccountModule, BankAccountModule>()
            .AddScoped<IBankAccountAdminModule, BankAccountAdminModule>()
            .AddScoped<ITransactionModule, TransactionModule>();
        
        return services;
    }
}