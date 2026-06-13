using System.Net.Mail;
using Microsoft.Extensions.Options;
using OrderHub.Application.Interfaces;

namespace OrderHub.Infrastructure.Notifications;

public sealed record SmtpOptions(string Host, int Port, string FromAddress);

public sealed class SmtpNotificationService(IOptions<SmtpOptions> opts) : INotificationService
{
    public async Task SendOrderConfirmationAsync(string parentEmail, string orderReference, decimal total, CancellationToken ct = default)
    {
        using var client  = new SmtpClient(opts.Value.Host, opts.Value.Port);
        using var message = new MailMessage(
            from: opts.Value.FromAddress, 
            to: parentEmail,
            subject: $"Order confirmed — ref {orderReference}", 
            body: $"Your order has been confirmed. Total: £{total:F2}");

        await client.SendMailAsync(message, ct);
    }
}
