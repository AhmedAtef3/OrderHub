using OrderHub.Contracts.Orders;

namespace OrderHub.OrderService.Repositories;

public interface IOrderRepository
{
    Task<int> CreateDraftAsync(CreateOrderRequest request, CancellationToken ct);
    Task UpdateStatusAsync(int orderId, string status, CancellationToken ct);
    Task SetReservationIdAsync(int orderId, string reservationId, CancellationToken ct);
    Task SetPaymentIntentIdAsync(int orderId, string intentId, CancellationToken ct);
    Task<OrderSagaState?> GetStatusAsync(int orderId, CancellationToken ct);
}

public sealed record OrderSagaState(int OrderId, int SchoolId, string ParentEmail, string Status,string? ReservationId,
    string? PaymentIntentId, decimal? Total);
