using OrderHub.NotificationService.Email;
using OrderHub.NotificationService.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderConfirmedWorker>();
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();

var host = builder.Build();
host.Run();
