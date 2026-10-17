using Microsoft.EntityFrameworkCore;
using SafeRoute.OrderService.Data;
using SafeRoute.OrderService.Models;
using System.ComponentModel.DataAnnotations;
using SafeRoute.Shared.Events;
using MassTransit;
using Serilog;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/order-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddValidation(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация DbContext
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var rabbitMqUri = builder.Configuration.GetConnectionString("RabbitMq")
    ?? "amqp://guest:guest@localhost:5672/";

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitMqUri));
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
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

// включаем сваггер в девелопмент режиме
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.MapPost("/orders", async (OrderRequest request, OrderDbContext db,
 IPublishEndpoint publishEndpoint) =>
{
    var order = new Order
    {
        Id = Guid.NewGuid(),
        ProductName = request.ProductName,
        Quantity = request.Quantity,
        CreatedAt = DateTime.UtcNow
    };

    db.Orders.Add(order);

    // Сначала Publish — MassTransit перехватит и положит событие в Outbox (не в RabbitMQ напрямую)
    await publishEndpoint.Publish(new OrderCreatedEvent
    {
        OrderId = order.Id,
        ProductName = order.ProductName,
        Quantity = order.Quantity,
        CreatedAt = order.CreatedAt
    });

    // Потом SaveChanges — атомарно сохранит и заказ, и сообщение в Outbox в одной транзакции
    await db.SaveChangesAsync();

    var response = new OrderResponse(order.Id, order.ProductName, order.Quantity, order.CreatedAt);
    return Results.Ok(response);
});


app.MapGet("/orders/id:{guid}", async (Guid id, OrderDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order == null) return Results.NotFound();
    var response = new OrderResponse(order.Id, order.ProductName, order.Quantity, order.CreatedAt);
    return Results.Ok(response);
}
);

app.MapGet("/orders", async (OrderDbContext db) =>
{
    var orders = await db.Orders
        .OrderByDescending(o => o.CreatedAt)
        .Select(o => new OrderResponse(o.Id, o.ProductName, o.Quantity, o.CreatedAt))
        .ToListAsync();
    
    return Results.Ok(orders);
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

// класс запроса от клиента
public record OrderRequest(
    [Required] // обязательно
    [StringLength(100, MinimumLength = 1)] // длина от 1 до 100 чаров
    string ProductName,

    [Range(1, int.MaxValue)] // quantity от 1 до максимального инта
     int Quantity);
//класс ответа от сервера
public record OrderResponse(Guid Id, string ProductName, int Quantity, DateTime CreatedAt);
