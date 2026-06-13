using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application;
using OrderHub.Application.Interfaces;
using OrderHub.Core.Abstractions;
using OrderHub.Infrastructure.Data;
using OrderHub.Infrastructure.Notifications;
using OrderHub.Infrastructure.Payment;

namespace OrderHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderHub(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("OrderHub") ?? throw new InvalidOperationException("Connection string 'OrderHub' is required.");

        services.Configure<DatabaseOptions>(o => o = new DatabaseOptions(connStr));

        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));

        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockRepository, StockRepository>();

        services.AddHttpClient<IPaymentService, PaymentProviderService>(client =>
        {
            var baseUrl = configuration["PaymentProvider:BaseUrl"] ?? throw new InvalidOperationException("PaymentProvider:BaseUrl is required.");
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddScoped<INotificationService, SmtpNotificationService>();

        services.AddApplicationServices();

        return services;
    }
}
