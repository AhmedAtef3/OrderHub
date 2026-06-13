using System.Net.Http.Json;
using OrderHub.Contracts.Payment;

namespace OrderHub.PaymentService.ExternalProvider;

public sealed class HttpPaymentProvider(HttpClient http, IConfiguration config) : IExternalPaymentProvider
{
    public async Task<string> CreateIntentAsync(decimal amount, string email, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("intents", new { amount, email }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProviderIntentResult>(ct);
        return result!.Id;
    }

    public bool VerifySignature(PaymentWebhookPayload payload, string signature)
    {
        var secret = config["PaymentProvider:WebhookSecret"]!;
        return !string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(signature);
    }

    private sealed record ProviderIntentResult(string Id);
}
