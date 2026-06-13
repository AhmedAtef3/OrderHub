using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace OrderHub.OrderService.Broker;

public sealed class ServiceBusPublisher(IConfiguration config) : IBrokerPublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : class
    {
        await using var client = new ServiceBusClient(config["ServiceBus:ConnectionString"]!);
        var sender = client.CreateSender(typeof(T).Name.ToLowerInvariant());
        await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(message)), ct);
    }
}
