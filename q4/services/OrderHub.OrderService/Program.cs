using OrderHub.OrderService.Broker;
using OrderHub.OrderService.HttpClients;
using OrderHub.OrderService.Repositories;
using OrderHub.OrderService.Saga;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddHttpClient<IPricingClient, PricingClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Pricing"]!));
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Inventory"]!));
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Payment"]!));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IBrokerPublisher, ServiceBusPublisher>();
builder.Services.AddScoped<OrderSaga>();

var app = builder.Build();
app.MapControllers();
app.Run();
