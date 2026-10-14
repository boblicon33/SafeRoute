var builder = WebApplication.CreateBuilder(args);

// 1. РЕГИСТРАЦИЯ SWAGGER (Добавить эти строки)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. ВКЛЮЧЕНИЕ SWAGGER (Добавить эти строки)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();

// Наш первый эндпоинт для создания заказа
app.MapPost("/orders", (OrderRequest request) =>
{
    // Пока просто возвращаем то, что получили, добавляя ID
    var order = new OrderResponse(
        Guid.NewGuid(),
        request.ProductName,
        request.Quantity,
        DateTime.UtcNow
    );
    
    return Results.Ok(order); 
});

app.Run();

// класс запроса от клиента
public record OrderRequest(string ProductName, int Quantity);
//класс ответа от сервера
public record OrderResponse(Guid Id, string ProductName, int Quantity, DateTime CreatedAt);
