using OrderHub.Core.Models;

namespace OrderHub.Core.Abstractions;

public interface ISchoolRepository
{
    Task<PricingTier?> GetPricingTierAsync(int schoolId, CancellationToken ct = default);
}

public interface IProductRepository
{
    Task<decimal?> GetBasePriceAsync(string sku, CancellationToken ct = default);
}

public interface IStockRepository
{
    Task<int> GetAvailableStockAsync(string sku, CancellationToken ct = default);
}
