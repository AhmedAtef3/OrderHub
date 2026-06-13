using OrderHub.Contracts.Inventory;
using OrderHub.Contracts.Payment;
using OrderHub.Contracts.Pricing;

namespace OrderHub.OrderService.HttpClients;

public interface IPricingClient
{
    Task<PriceOrderResponse> PriceOrderAsync(PriceOrderRequest request, CancellationToken ct);
}

public interface IInventoryClient
{
    Task<ReserveStockResponse> ReserveAsync(ReserveStockRequest request, CancellationToken ct);
    Task CommitAsync(string reservationId, CancellationToken ct);
    Task ReleaseAsync(string reservationId, CancellationToken ct);
}

public interface IPaymentClient
{
    Task<CreateIntentResponse> CreateIntentAsync(CreateIntentRequest request, CancellationToken ct);
}
