namespace OrderHub.Contracts.Payment;

public sealed record CreateIntentRequest(decimal Amount, string ParentEmail, int OrderId, string IdempotencyKey);

public sealed record CreateIntentResponse(string IntentId);

public sealed record PaymentWebhookPayload(string IntentId, string Status);
