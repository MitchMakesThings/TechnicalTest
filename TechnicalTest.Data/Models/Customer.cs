namespace TechnicalTest.Data.Models;

public class Customer : BaseEntity
{
    public required string Name { get; set; }
    public ICollection<BankAccount> BankAccounts { get; set; } = new HashSet<BankAccount>();
    
    public DateOnly DateOfBirth { get; set; }
    public required decimal DailyLimit { get; set; }
}