namespace TechnicalTest.Data.Modules.DTOs;

public record CustomerCreationDto(string? Name, DateOnly? DateOfBirth, decimal? DailyLimit, decimal InitialBalance);