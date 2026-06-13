namespace OrderHub.PaymentService.Repositories;

public interface IPaymentRepository
{
    Task         SaveIntentAsync(string intentId, int orderId, decimal amount, CancellationToken ct);
    Task<string?> GetIntentByOrderIdAsync(int orderId, CancellationToken ct);
    Task<int?>    GetOrderIdByIntentAsync(string intentId, CancellationToken ct);
}
