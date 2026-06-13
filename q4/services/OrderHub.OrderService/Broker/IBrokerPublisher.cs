namespace OrderHub.OrderService.Broker;

public interface IBrokerPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct) where T : class;
}
