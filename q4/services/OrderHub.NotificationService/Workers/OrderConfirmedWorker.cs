using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OrderHub.Contracts.Events;
using OrderHub.NotificationService.Email;

namespace OrderHub.NotificationService.Workers;

public sealed class OrderConfirmedWorker(IConfiguration config, IServiceProvider services, ILogger<OrderConfirmedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var client = new ServiceBusClient(config["ServiceBus:ConnectionString"]!);
        await using var processor = client.CreateProcessor(
            topicName: "orderconfirmed",
            subscriptionName: config["ServiceBus:SubscriptionName"] ?? "notification-service",
            options: new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls   = 4,
                AutoCompleteMessages = false
            });

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
        await processor.StopProcessingAsync();
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var confirmed = JsonSerializer.Deserialize<OrderConfirmed>(args.Message.Body)!;
            logger.LogInformation("Sending confirmation for order {OrderId}", confirmed.OrderId);

            using var scope = services.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            await emailSender.SendAsync(
                to:      confirmed.ParentEmail,
                subject: $"Order confirmed — ref {confirmed.OrderId}",
                body:    $"Your order has been confirmed. Total: £{confirmed.Total:F2}",
                ct:      args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order confirmation message");
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus processor error on {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }
}
