namespace TechnicalTest.Data.Modules.DTOs;

public record TransactionCreationDto(int DebitAccountId, int CreditAccountId, decimal Amount, string? Reference);