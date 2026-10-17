using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using SafeRoute.DeliveryService.Consumers;
using SafeRoute.DeliveryService.Data;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqUri = builder.Configuration.GetConnectionString("RabbitMq")
    ?? "amqp://guest:guest@localhost:5672/";

builder.Services.AddDbContext<DeliveryDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsHistoryTable("__EFMigrationsHistory", "delivery")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedEventConsumer>();

    x.AddEntityFrameworkOutbox<DeliveryDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitMqUri));

        // обязательно — иначе очередь консьюмера не создастся
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
    var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    db.Database.Migrate();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();