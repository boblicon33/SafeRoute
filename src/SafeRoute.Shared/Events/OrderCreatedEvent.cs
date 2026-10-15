namespace SafeRoute.Shared.Events;

public record OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public DateTime CreatedAt { get; init; }
}