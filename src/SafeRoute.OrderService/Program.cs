using Microsoft.EntityFrameworkCore;
using SafeRoute.OrderService.Data;
using SafeRoute.OrderService.Models;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация DbContext
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// включаем сваггер в девелопмент режиме
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();

app.MapPost("/orders", async (OrderRequest request, OrderDbContext db) =>
{
    var order = new Order
    {
        Id = Guid.NewGuid(),
        ProductName = request.ProductName,
        Quantity = request.Quantity,
        CreatedAt = DateTime.UtcNow
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    var response = new OrderResponse(
        order.Id,
        order.ProductName,
        order.Quantity,
        order.CreatedAt
    );

    return Results.Ok(response);
});

app.Run();

// класс запроса от клиента
public record OrderRequest(string ProductName, int Quantity);
//класс ответа от сервера
public record OrderResponse(Guid Id, string ProductName, int Quantity, DateTime CreatedAt);
