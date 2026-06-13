using Microsoft.Extensions.Logging;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Services;
using OrderHub.Core.Abstractions;
using OrderHub.Core.Models;

namespace OrderHub.Application.UseCases;

/// <summary>
/// USE CASE: Process a back-to-school order.
///
/// This class owns the entire order pipeline. It is the answer to:
/// "What does the system DO when a parent places an order?"
///
/// Pipeline:
///   1. Validate school exists and resolve pricing tier
///   2. For each line: resolve base price → apply tier + embroidery pricing → check stock
///   3. Create payment intent
///   4. Send confirmation (fire-and-forget — never fails the order)
///
/// Depends on:
///   Core repositories   → ISchoolRepository, IProductRepository, IStockRepository
///   Application ports   → IPaymentService, INotificationService
///   Application service → PricingCalculator (pure, no injection needed)
///
/// Does NOT depend on:
///   SQL, HttpClient, SmtpClient — those are Infrastructure concerns.
///   Razor Pages, controllers   — those are Web concerns.
/// </summary>
public sealed class ProcessOrderUseCase(
    ISchoolRepository    schools,
    IProductRepository   products,
    IStockRepository     stock,
    IPaymentService      payments,
    INotificationService notifications,
    ILogger<ProcessOrderUseCase> logger)
{
    public async Task<OrderResult> ExecuteAsync(
        int             schoolId,
        List<OrderLine> lines,
        string          parentEmail,
        CancellationToken ct = default)
    {
        // ── Step 1: Resolve school pricing tier ───────────────────────────────
        var tier = await schools.GetPricingTierAsync(schoolId, ct);
        if (tier is null)
        {
            logger.LogWarning("Order rejected — school {SchoolId} not found", schoolId);
            return new OrderResult.Failure("school not found");
        }

        // ── Step 2: Price and stock-check each line ───────────────────────────
        decimal subtotal = 0m;

        foreach (var line in lines)
        {
            var basePrice = await products.GetBasePriceAsync(line.Sku, ct);
            if (basePrice is null)
            {
                logger.LogWarning("Order rejected — unknown SKU {Sku}", line.Sku);
                return new OrderResult.Failure($"unknown product {line.Sku}");
            }

            // Business rule: apply tier discount then embroidery surcharge
            var unitPrice = PricingCalculator.CalculateUnitPrice(basePrice.Value, tier.Value, line.Embroidery);

            // Business rule: stock must cover the requested quantity
            var available = await stock.GetAvailableStockAsync(line.Sku, ct);
            if (available < line.Quantity)
            {
                logger.LogWarning(
                    "Order rejected — insufficient stock for {Sku}: requested {Req}, available {Avail}",
                    line.Sku, line.Quantity, available);
                return new OrderResult.Failure($"out of stock {line.Sku}");
            }

            subtotal += unitPrice * line.Quantity;
        }

        // ── Step 3: Create payment intent ─────────────────────────────────────
        // IdempotencyKey = schoolId + email + timestamp bucket prevents double-charge
        // if the caller retries within the same minute window.
        string orderReference;
        try
        {
            var idempotencyKey = $"{schoolId}:{parentEmail}:{DateTime.UtcNow:yyyyMMddHHmm}";
            orderReference = await payments.CreatePaymentIntentAsync(subtotal, parentEmail, idempotencyKey, ct);
        }
        catch (PaymentException ex)
        {
            logger.LogError(ex, "Payment provider rejected intent for {Email}", parentEmail);
            return new OrderResult.Failure("payment provider error");
        }

        // ── Step 4: Send confirmation (fire-and-forget) ───────────────────────
        // A failed notification must never cancel a successfully paid order.
        _ = SendConfirmationSafelyAsync(parentEmail, orderReference, subtotal);

        logger.LogInformation(
            "Order {Reference} confirmed for school {SchoolId}, total £{Total:F2}",
            orderReference, schoolId, subtotal);

        return new OrderResult.Success(orderReference, subtotal);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task SendConfirmationSafelyAsync(string email, string reference, decimal total)
    {
        try
        {
            await notifications.SendOrderConfirmationAsync(email, reference, total);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Confirmation email failed for {Email} / order {Reference} — order is still confirmed",
                email, reference);
        }
    }
}
