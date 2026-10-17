using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using SafeRoute.MonitorService.Consumers;
using SafeRoute.MonitorService.Data;
using SafeRoute.MonitorService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<TelegramNotifier>();

builder.Services.AddDbContext<MonitorDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsHistoryTable("__EFMigrationsHistory", "monitor")));

var rabbitMqUri = builder.Configuration.GetConnectionString("RabbitMq")
    ?? "amqp://guest:guest@localhost:5672/";

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
        cfg.Host(new Uri(rabbitMqUri));

        // обязательно — иначе очереди консьюмеров не создадутся
        cfg.ConfigureEndpoints(context);
    });
});

var pgConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var rabbitConnection = builder.Configuration.GetConnectionString("RabbitMq") ?? string.Empty;

builder.Services.AddHealthChecks()
    .AddNpgSql(pgConnection, name: "postgres")
    .AddRabbitMQ(_ => new ConnectionFactory { Uri = new Uri(rabbitConnection) }.CreateConnectionAsync(), name: "rabbitmq");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
    db.Database.Migrate();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();