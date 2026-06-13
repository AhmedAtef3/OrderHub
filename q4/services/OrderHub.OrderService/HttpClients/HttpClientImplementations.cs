using System.Net.Http.Json;
using OrderHub.Contracts.Inventory;
using OrderHub.Contracts.Payment;
using OrderHub.Contracts.Pricing;

namespace OrderHub.OrderService.HttpClients;

public sealed class PricingClient(HttpClient http) : IPricingClient
{
    public async Task<PriceOrderResponse> PriceOrderAsync(PriceOrderRequest request, CancellationToken ct)
        => await http.PostAsJsonAsync("price-order", request, ct).ReadJsonAsync<PriceOrderResponse>(ct);
}

public sealed class InventoryClient(HttpClient http) : IInventoryClient
{
    public async Task<ReserveStockResponse> ReserveAsync(ReserveStockRequest request, CancellationToken ct)
        => await http.PostAsJsonAsync("reserve", request, ct).ReadJsonAsync<ReserveStockResponse>(ct);
    public async Task CommitAsync(string reservationId, CancellationToken ct)
        => await http.PatchAsync($"reserve/{reservationId}/commit", null, ct);
    public async Task ReleaseAsync(string reservationId, CancellationToken ct)
        => await http.DeleteAsync($"reserve/{reservationId}", ct);
}

public sealed class PaymentClient(HttpClient http) : IPaymentClient
{
    public async Task<CreateIntentResponse> CreateIntentAsync(CreateIntentRequest request, CancellationToken ct)
        => await http.PostAsJsonAsync("intents", request, ct).ReadJsonAsync<CreateIntentResponse>(ct);
}

internal static class HttpResponseExtensions
{
    public static async Task<T> ReadJsonAsync<T>(this Task<HttpResponseMessage> responseTask, CancellationToken ct)
    {
        var response = await responseTask;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new InvalidOperationException("Empty response from service");
    }
}
