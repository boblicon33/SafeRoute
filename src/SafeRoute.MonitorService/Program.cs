using MassTransit;
using Microsoft.EntityFrameworkCore;
using SafeRoute.MonitorService.Consumers;
using SafeRoute.MonitorService.Data;
using SafeRoute.MonitorService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<TelegramNotifier>();

builder.Services.AddDbContext<MonitorDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsHistoryTable("__EFMigrationsHistory", "monitor")));
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DeliveryFailedEventConsumer>();
    x.AddConsumer<DeliveryProcessedEventConsumer>();

    x.AddEntityFrameworkOutbox<MonitorDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // обязательно — иначе очереди консьюмеров не создадутся
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.Run();