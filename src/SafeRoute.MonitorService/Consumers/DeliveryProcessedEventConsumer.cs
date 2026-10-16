using MassTransit;
using SafeRoute.Shared.Events;

namespace SafeRoute.MonitorService.Consumers;

public class DeliveryProcessedEventConsumer : IConsumer<DeliveryProcessedEvent>
{
    private readonly ILogger<DeliveryProcessedEventConsumer> _logger;

    public DeliveryProcessedEventConsumer(ILogger<DeliveryProcessedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DeliveryProcessedEvent> context)
    {
        _logger.LogInformation("Delivery processed for order {OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}