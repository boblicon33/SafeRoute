using MassTransit;
using Microsoft.EntityFrameworkCore;
using SafeRoute.DeliveryService.Models;

namespace SafeRoute.DeliveryService.Data;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<Delivery> Deliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // вся схема DeliveryService (Deliveries + outbox) — в отдельной схеме "delivery"
        modelBuilder.HasDefaultSchema("delivery");

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}