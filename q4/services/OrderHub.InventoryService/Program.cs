using OrderHub.InventoryService.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

var app = builder.Build();
app.MapControllers();
app.Run();
