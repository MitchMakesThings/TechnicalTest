namespace TechnicalTest.Data.Models;

public class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } =  DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}