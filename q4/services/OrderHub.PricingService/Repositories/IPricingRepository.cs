using OrderHub.Core.Models;

namespace OrderHub.PricingService.Repositories;

public interface IPricingRepository
{
    Task<PricingTier?> GetPricingTierAsync(int schoolId, CancellationToken ct);
    Task<decimal?> GetBasePriceAsync(string sku, CancellationToken ct);
}
