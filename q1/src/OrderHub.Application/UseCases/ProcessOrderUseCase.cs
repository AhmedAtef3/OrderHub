using Microsoft.Extensions.Logging;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Services;
using OrderHub.Core.Abstractions;
using OrderHub.Core.Models;

namespace OrderHub.Application.UseCases;

public sealed class ProcessOrderUseCase(ISchoolRepository schools,IProductRepository products, IStockRepository stock,
    IPaymentService payments, INotificationService notifications) : IProcessOrderUseCase
{
    public async Task<OrderResult> ExecuteAsync(int schoolId, List<OrderLine> lines, string parentEmail, CancellationToken ct = default)
    {
        var tier = await schools.GetPricingTierAsync(schoolId, ct);
        if (tier is null)
        {
            return new OrderResult.Failure("school not found");
        }

        decimal subtotal = 0m;
        foreach (var line in lines)
        {
            var basePrice = await products.GetBasePriceAsync(line.Sku, ct);
            if (basePrice is null)
            {
                return new OrderResult.Failure($"unknown product {line.Sku}");
            }

            var unitPrice = PricingCalculator.CalculateUnitPrice(basePrice.Value, tier.Value, line.Embroidery);

            var available = await stock.GetAvailableStockAsync(line.Sku, ct);
            if (available < line.Quantity)
            {
                return new OrderResult.Failure($"out of stock {line.Sku}");
            }

            subtotal += unitPrice * line.Quantity;
        }

        string orderReference;
        try
        {
            var idempotencyKey = $"{schoolId}:{parentEmail}:{DateTime.UtcNow:yyyyMMddHHmm}";
            orderReference = await payments.CreatePaymentIntentAsync(subtotal, parentEmail, idempotencyKey, ct);
        }
        catch (PaymentException ex)
        {
            return new OrderResult.Failure("payment provider error");
        }

        _ = SendConfirmationSafelyAsync(parentEmail, orderReference, subtotal);

        return new OrderResult.Success(orderReference, subtotal);
    }

    private async Task SendConfirmationSafelyAsync(string email, string reference, decimal total)
    {
        await notifications.SendOrderConfirmationAsync(email, reference, total);
    }
}
