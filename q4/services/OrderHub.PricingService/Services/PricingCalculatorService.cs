using Microsoft.Extensions.Logging;
using OrderHub.Application.Services;
using OrderHub.Contracts.Pricing;
using OrderHub.Core.Models;
using OrderHub.PricingService.Repositories;

namespace OrderHub.PricingService.Services;

public sealed class PricingCalculatorService(IPricingRepository repo)
{
    public async Task<PriceOrderResponse?> PriceAsync(PriceOrderRequest request, CancellationToken ct)
    {
        var tier = await repo.GetPricingTierAsync(request.SchoolId, ct);
        if (tier is null)
        {
            return null;
        }

        var pricedLines = new List<PricedLine>();
        decimal subtotal = 0;

        foreach (var line in request.Lines)
        {
            var basePrice = await repo.GetBasePriceAsync(line.Sku, ct);
            if (basePrice is null)
            {
                return null;
            }
            var unit = PricingCalculator.CalculateUnitPrice(basePrice.Value, tier.Value, line.Embroidery);
            var lineTotal = unit * line.Quantity;
            pricedLines.Add(new PricedLine(line.Sku, unit, lineTotal));
            subtotal += lineTotal;
        }

        return new PriceOrderResponse(pricedLines, subtotal);
    }
}
