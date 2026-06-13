using MailKit.Net.Smtp;
using MimeKit;

namespace OrderHub.NotificationService.Email;

public sealed class MailKitEmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(config["Smtp:FromAddress"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(config["Smtp:Host"], int.Parse(config["Smtp:Port"]!),
            MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(config["Smtp:User"], config["Smtp:Password"], ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
