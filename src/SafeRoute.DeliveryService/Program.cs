using MassTransit;
using Microsoft.EntityFrameworkCore;
using SafeRoute.DeliveryService.Consumers;
using SafeRoute.DeliveryService.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<DeliveryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // обязательно — иначе очередь консьюмера не создастся
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.Run();