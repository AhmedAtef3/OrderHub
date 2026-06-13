using OrderHub.PaymentService.Broker;
using OrderHub.PaymentService.ExternalProvider;
using OrderHub.PaymentService.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient<IExternalPaymentProvider, HttpPaymentProvider>(c =>
    c.BaseAddress = new Uri(builder.Configuration["PaymentProvider:BaseUrl"]!));
builder.Services.AddScoped<IPaymentRepository, SqlPaymentRepository>();
builder.Services.AddScoped<IPaymentBroker, ServiceBusPaymentBroker>();

var app = builder.Build();
app.MapControllers();
app.Run();
