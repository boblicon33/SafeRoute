using MassTransit;
using SafeRoute.MonitorService.Monitoring;
using SafeRoute.Shared.Events;

namespace SafeRoute.MonitorService.Consumers;

public class DeliveryProcessedEventConsumer : IConsumer<DeliveryProcessedEvent>
{
    public Task Consume(ConsumeContext<DeliveryProcessedEvent> context)
    {
        Interlocked.Exchange(ref DeliveryFailureState.Count, 0);
        return Task.CompletedTask;
    }
}