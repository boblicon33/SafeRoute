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
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCreatedEventConsumer(DeliveryDbContext db, ILogger<OrderCreatedEventConsumer> logger, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
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
            if (attempt <= 3)
                throw new Exception("Delivery service API is unavailable");

            delivery.Status = "Processed";
        }
        catch (Exception ex)
        {
            delivery.Status = "Failed";
            delivery.FailureReason = ex.Message;
            _logger.LogError(ex, "Delivery failed for order {OrderId}", message.OrderId);
        }

        if (delivery.Status == "Processed")
        {
            await _publishEndpoint.Publish(new DeliveryProcessedEvent
            {
                OrderId = message.OrderId,
                ProcessedAt = delivery.ProcessedAt
            });
        }
        else
        {
            await _publishEndpoint.Publish(new DeliveryFailedEvent
            {
                OrderId = message.OrderId,
                Reason = delivery.FailureReason ?? string.Empty,
                FailedAt = delivery.ProcessedAt
            });
        }

        _db.Deliveries.Add(delivery);
        await _db.SaveChangesAsync();
    }
}