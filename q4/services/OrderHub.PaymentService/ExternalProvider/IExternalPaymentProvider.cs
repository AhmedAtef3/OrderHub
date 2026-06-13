using OrderHub.Contracts.Payment;

namespace OrderHub.PaymentService.ExternalProvider;

public interface IExternalPaymentProvider
{
    Task<string> CreateIntentAsync(decimal amount, string email, CancellationToken ct);
    bool VerifySignature(PaymentWebhookPayload payload, string signature);
}
