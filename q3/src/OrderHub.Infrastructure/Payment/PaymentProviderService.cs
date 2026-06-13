using System.Net.Http.Json;
using OrderHub.Application.Interfaces;
using OrderHub.Core.Abstractions;

namespace OrderHub.Infrastructure.Payment;

public sealed class PaymentProviderService(HttpClient http) : IPaymentService
{
    public async Task<string> CreatePaymentIntentAsync(decimal amount, string  parentEmail, string  idempotencyKey, CancellationToken ct = default)
    {
        var payload = new { amount, email = parentEmail };

        using var request = new HttpRequestMessage(HttpMethod.Post, "intents")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new PaymentException($"Provider returned {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>(ct)
            ?? throw new PaymentException("Provider returned an empty response");

        return result.Reference;
    }

    private sealed record PaymentIntentResponse(string Reference);
}
