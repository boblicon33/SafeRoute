namespace SafeRoute.DeliveryService.Models;

public class Delivery
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty; // "Processed" | "Failed"
    public DateTime ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}