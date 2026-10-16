using MassTransit;
using SafeRoute.DeliveryService.Data;
using SafeRoute.DeliveryService.Models;
using SafeRoute.Shared.Events;

namespace SafeRoute.DeliveryService.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private static int _receivedCount;

    private readonly DeliveryDbContext _db;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(DeliveryDbContext db, ILogger<OrderCreatedEventConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            ProductName = message.ProductName,
            Quantity = message.Quantity,
            ProcessedAt = DateTime.UtcNow
        };

        var attempt = Interlocked.Increment(ref _receivedCount);

        try
        {
            if (attempt % 5 == 0)
                throw new Exception("Delivery service API is unavailable");

            delivery.Status = "Processed";
        }
        catch (Exception ex)
        {
            delivery.Status = "Failed";
            delivery.FailureReason = ex.Message;
            _logger.LogError(ex, "Delivery failed for order {OrderId}", message.OrderId);
        }

        _db.Deliveries.Add(delivery);
        await _db.SaveChangesAsync();
    }
}