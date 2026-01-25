using Microsoft.EntityFrameworkCore;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data;

public class ApplicationContext : DbContext
{
    public ApplicationContext()
    {
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=bin\\database.db;");
        }
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<BankAccount>()
            .HasAlternateKey(a => a.AccountNumber);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.DebitBankAccount)
            .WithMany(b => b.DebitTransactions);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.BaseType != typeof(BaseEntity))
            {
                continue;
            }

            if (entityType.IsKeyless)
            {
                entityType.AddKey(entityType.GetProperty(nameof(BaseEntity.Id)));
            }

            if (Database.IsSqlite())
            {
                // ASSUMPTION: We would replace SQLite with a suitable database server ASAP! Until then, here's some workarounds for its limitations
                // Interestingly, migrations built against SQLite don't automagically populate the default value like they would against MSSQL/Postrgres
                // So we'll manually set the default value here.
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("sysdatetimeoffset()");
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.UpdatedAt))
                    .HasDefaultValueSql("sysdatetimeoffset()");
            }
        }
    }
    
    
    public override int SaveChanges()
    {
        UpdateLastUpdated();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateLastUpdated();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Update the timestamp whenever entities are saved.
    /// </summary>
    private void UpdateLastUpdated()
    {
        foreach(var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList())
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTimeOffset.UtcNow;
                // TODO I guess in a banking type context we'd log audit entries including the user who made the change etc, so this would be quite different
            }
        }
    }

    
}