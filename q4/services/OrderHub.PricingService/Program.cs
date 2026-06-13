using OrderHub.PricingService.Repositories;
using OrderHub.PricingService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<IPricingRepository, PricingRepository>();
builder.Services.AddScoped<PricingCalculatorService>();

var app = builder.Build();
app.MapControllers();
app.Run();
