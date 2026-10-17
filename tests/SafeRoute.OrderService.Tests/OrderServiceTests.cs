using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafeRoute.OrderService.Data;
using Xunit;

namespace SafeRoute.OrderService.Tests;

public class OrderServiceTests : IClassFixture<OrderServiceFactory>
{
    private readonly OrderServiceFactory _factory;
    private readonly HttpClient _client;

    public OrderServiceTests(OrderServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsOk_AndSavesToDatabase()
    {
        var response = await _client.PostAsJsonAsync("/orders", new OrderRequest("Test", 5));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.SingleAsync(o => o.ProductName == "Test");
        order.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task CreateOrder_WithZeroQuantity_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/orders", new OrderRequest("Test", 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrder_ById_ReturnsCreatedOrder()
    {
        var create = await _client.PostAsJsonAsync("/orders", new OrderRequest("GetMe", 2));
        var created = await create.Content.ReadFromJsonAsync<OrderResponse>();

        var get = await _client.GetAsync($"/orders/{created!.Id}");

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await get.Content.ReadFromJsonAsync<OrderResponse>();
        result!.ProductName.Should().Be("GetMe");
        result.Quantity.Should().Be(2);
    }
}