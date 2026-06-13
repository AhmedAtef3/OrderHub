namespace OrderHub.PaymentService.Broker;

public interface IPaymentBroker
{
    Task PublishAsync<T>(T message, CancellationToken ct) where T : class;
}
