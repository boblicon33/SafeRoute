using MassTransit;
using SafeRoute.MonitorService.Data;
using SafeRoute.MonitorService.Models;
using SafeRoute.MonitorService.Monitoring;
using SafeRoute.MonitorService.Services;
using SafeRoute.Shared.Events;

namespace SafeRoute.MonitorService.Consumers;

public class DeliveryFailedEventConsumer : IConsumer<DeliveryFailedEvent>
{
    private readonly MonitorDbContext _db;
    private readonly TelegramNotifier _telegramNotifier;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeliveryFailedEventConsumer> _logger;

    public DeliveryFailedEventConsumer(
        MonitorDbContext db,
        TelegramNotifier telegramNotifier,
        IConfiguration configuration,
        ILogger<DeliveryFailedEventConsumer> logger)
    {
        _db = db;
        _telegramNotifier = telegramNotifier;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeliveryFailedEvent> context)
    {
        var message = context.Message;
        var count = Interlocked.Increment(ref DeliveryFailureState.Count);
        var threshold = _configuration.GetValue<int>("Monitor:FailureThreshold");

        if (count >= threshold)
        {
            var text = $"🚨 DeliveryService failed {count} times. Last order: {message.OrderId}. Reason: {message.Reason}.";

            var alertSent = false;
            try
            {
                await _telegramNotifier.SendAsync(text);
                alertSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram alert");
            }

            _db.Incidents.Add(new Incident
            {
                Id = Guid.NewGuid(),
                DetectedAt = DateTime.UtcNow,
                FailureCount = count,
                Message = text,
                AlertSent = alertSent
            });
            await _db.SaveChangesAsync();

            // сброс после алерта — следующее накопление N падений вызовет новый алерт
            Interlocked.Exchange(ref DeliveryFailureState.Count, 0);
        }
    }
}