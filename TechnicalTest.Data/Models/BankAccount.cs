namespace TechnicalTest.Data.Models;

public class BankAccount : BaseEntity
{
    // ASSUMPTION: spec should have a length limit of some sort. Leaving it out for the tech test
    // eg, [MaxLength(20)] and the migration to apply it
    public required string AccountNumber { get; init; }
    
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public DateTimeOffset? FrozenAt { get; set; }
    public decimal Balance { get; set; } // It is assumed some daily/regular reconciliation process will check/update this instead of summing the complete history of transactions

    public ICollection<Transaction> DebitTransactions { get; set; } = null!;
}