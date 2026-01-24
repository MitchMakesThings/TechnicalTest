namespace TechnicalTest.API.DTOs;

public record ApiResponse<T>  (bool Success, T? Data, string? Message = null, IEnumerable<int>? ErrorCodes = null);