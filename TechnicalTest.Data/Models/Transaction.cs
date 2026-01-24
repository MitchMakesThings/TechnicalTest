namespace TechnicalTest.Data.Models;

public class Transaction : BaseEntity
{
    public int DebitBankAccountId { get; set; }
    public BankAccount? DebitBankAccount { get; set; }
    
    public int CreditBankAccountId { get; set; }
    public BankAccount? CreditBankAccount { get; set; }
    
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}