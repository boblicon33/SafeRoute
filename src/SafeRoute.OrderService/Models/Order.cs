namespace SafeRoute.OrderService.Models;

// сущность заказа
public class Order
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}