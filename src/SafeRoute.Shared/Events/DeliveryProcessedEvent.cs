namespace SafeRoute.Shared.Events;

public record DeliveryProcessedEvent
{
    public Guid OrderId { get; init; }
    public DateTime ProcessedAt { get; init; }
}