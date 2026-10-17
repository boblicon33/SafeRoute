using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using SafeRoute.OrderService.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SafeRoute.OrderService.Tests;

public class OrderServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("saferoute_shop")
            .WithUsername("user")
            .WithPassword("root")
            .Build();

    public Mock<IPublishEndpoint> PublishEndpointMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // заменяем DbContext на контекст с Testcontainers (после AddDbContext из Program.cs)
            services.RemoveAll<DbContextOptions<OrderDbContext>>();
            services.RemoveAll<OrderDbContext>();
            services.AddDbContext<OrderDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // выключаем MassTransit и мокаем publisher, чтобы не ходить в RabbitMQ
            services.RemoveMassTransitHostedService();

            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IHostedService)
                                  && d.ImplementationType?.Assembly.GetName().Name?
                                      .StartsWith("MassTransit", StringComparison.Ordinal) == true)
                         .ToList())
                services.Remove(descriptor);

            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(PublishEndpointMock.Object);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}