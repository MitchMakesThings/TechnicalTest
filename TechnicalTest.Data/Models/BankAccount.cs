using System.ComponentModel.DataAnnotations;

namespace TechnicalTest.Data.Models;

public class BankAccount : BaseEntity
{
    // TODO should have a length limit of some sort.
    // eg, [MaxLength(20)] and the migration to apply it
    public required string AccountNumber { get; init; }
    
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public DateTimeOffset? FrozenAt { get; set; }
    public decimal Balance { get; set; }
}