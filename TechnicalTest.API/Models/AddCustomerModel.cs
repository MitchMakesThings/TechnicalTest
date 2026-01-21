namespace TechnicalTest.API.Models;

public record AddCustomerModel(string Name, DateOnly DateOfBirth, decimal DailyLimit = 10_000);