using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.UseCases;

namespace OrderHub.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Application-layer use cases.
    /// Call from Infrastructure's AddOrderHub() so the Web project
    /// only needs one registration call.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ProcessOrderUseCase>();
        return services;
    }
}
