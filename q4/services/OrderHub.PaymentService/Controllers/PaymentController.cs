using Microsoft.AspNetCore.Mvc;
using OrderHub.Contracts.Events;
using OrderHub.Contracts.Payment;
using OrderHub.PaymentService.Broker;
using OrderHub.PaymentService.ExternalProvider;
using OrderHub.PaymentService.Repositories;

namespace OrderHub.PaymentService.Controllers;

[ApiController]
public sealed class PaymentController(IExternalPaymentProvider provider, IPaymentRepository repo,
    IPaymentBroker broker) : ControllerBase
{
    [HttpPost("intents")]
    public async Task<IActionResult> CreateIntent(
        [FromBody] CreateIntentRequest request, CancellationToken ct)
    {
        var existing = await repo.GetIntentByOrderIdAsync(request.OrderId, ct);
        if (existing is not null)
            return Ok(new CreateIntentResponse(existing));

        var intentId = await provider.CreateIntentAsync(request.Amount, request.ParentEmail, ct);
        await repo.SaveIntentAsync(intentId, request.OrderId, request.Amount, ct);
        return Ok(new CreateIntentResponse(intentId));
    }

    [HttpPost("webhooks/payment")]
    public async Task<IActionResult> HandleWebhook([FromBody] PaymentWebhookPayload payload,
        [FromHeader(Name = "X-Webhook-Signature")] string signature, CancellationToken ct)
    {
        if (!provider.VerifySignature(payload, signature))
            return Unauthorized();

        var orderId = await repo.GetOrderIdByIntentAsync(payload.IntentId, ct);
        if (orderId is null) return NotFound();

        if (payload.Status == "succeeded")
            await broker.PublishAsync(new PaymentSucceeded(payload.IntentId, orderId.Value, Amount: 0), ct);
        else
            await broker.PublishAsync(new PaymentFailed(payload.IntentId, orderId.Value, payload.Status), ct);

        return Ok();
    }
}
