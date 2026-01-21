namespace TechnicalTest.Data.Models;

public class Transaction : BaseEntity
{
    public int FromBankAccountId { get; set; }
    public BankAccount? FromBankAccount { get; set; }
    
    public int ToBankAccountId { get; set; }
    public BankAccount? ToBankAccount { get; set; }
    
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}