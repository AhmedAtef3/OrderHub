using OrderHub.Core.Models;

namespace OrderHub.Application.Services;

public static class PricingCalculator
{
    private const decimal GoldMultiplier   = 0.85m;
    private const decimal SilverMultiplier = 0.92m;

    private const decimal EmbroideryShortSurcharge = 4.50m;
    private const decimal EmbroideryLongSurcharge  = 8.00m;
    private const int EmbroideryShortThreshold = 3;

    public static decimal CalculateUnitPrice(decimal basePrice, PricingTier tier, string? embroidery)
    {
        var price = ApplyTierDiscount(basePrice, tier);
        price = ApplyEmbroiderySurcharge(price, embroidery);
        return price;
    }

    private static decimal ApplyTierDiscount(decimal basePrice, PricingTier tier) =>
        tier switch
        {
            PricingTier.Gold => basePrice * GoldMultiplier,
            PricingTier.Silver => basePrice * SilverMultiplier,
            _ => basePrice
        };

    private static decimal ApplyEmbroiderySurcharge(decimal price, string? embroidery)
    {
        if (string.IsNullOrEmpty(embroidery)) return price;

        return embroidery.Length <= EmbroideryShortThreshold ? price + EmbroideryShortSurcharge : price + EmbroideryLongSurcharge;
    }
}
