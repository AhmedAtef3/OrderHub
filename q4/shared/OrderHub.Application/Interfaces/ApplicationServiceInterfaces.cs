namespace OrderHub.Application.Interfaces;

// ── Application-Level Service Interfaces ──────────────────────────────────────
// These are different from Core's repository interfaces.
// Repository interfaces describe persistence needs (get data).
// These describe external service needs (do something in the outside world).
// Both follow the same pattern: Application declares the interface,
// Infrastructure provides the implementation.

/// <summary>
/// Creates a payment intent with the external payment provider.
/// Application depends on this abstraction — it never touches HttpClient directly.
/// </summary>
public interface IPaymentService
{
    /// <param name="idempotencyKey">Passed to provider to prevent double-charging on retry.</param>
    /// <exception cref="Core.Abstractions.PaymentException">When the provider rejects the request.</exception>
    Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string  parentEmail,
        string  idempotencyKey,
        CancellationToken ct = default);
}

/// <summary>
/// Sends an order confirmation to the parent.
/// Fire-and-forget from the use case's perspective — failure here
/// must never cancel a successfully paid order.
/// </summary>
public interface INotificationService
{
    Task SendOrderConfirmationAsync(
        string  parentEmail,
        string  orderReference,
        decimal total,
        CancellationToken ct = default);
}
