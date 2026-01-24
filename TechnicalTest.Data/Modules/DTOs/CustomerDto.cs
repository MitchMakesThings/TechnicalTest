using System.Text.Json.Serialization;
using TechnicalTest.Data.Models;

namespace TechnicalTest.Data.Modules.DTOs;

[method: JsonConstructor]
public record CustomerDto(int Id, string Name, DateOnly DateOfBirth, decimal DailyLimit)
{
    public CustomerDto(Customer customer) : this(customer.Id, customer.Name, customer.DateOfBirth, customer.DailyLimit)
    {
    }

}