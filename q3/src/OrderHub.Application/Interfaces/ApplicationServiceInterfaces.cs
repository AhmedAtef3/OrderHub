namespace OrderHub.Application.Interfaces;

public interface IPaymentService
{
    Task<string> CreatePaymentIntentAsync(decimal amount, string  parentEmail, string  idempotencyKey, CancellationToken ct = default);
}

public interface INotificationService
{
    Task SendOrderConfirmationAsync(string  parentEmail, string  orderReference, decimal total, CancellationToken ct = default);
}
