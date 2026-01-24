namespace TechnicalTest.Data.Modules.DTOs;

public record CustomerModificationDto(string? Name, DateOnly? DateOfBirth, decimal? DailyLimit);