namespace OrderHub.Contracts.Events;


public sealed record PaymentSucceeded(string IntentId, int OrderId, decimal Amount);

public sealed record PaymentFailed(string IntentId, int OrderId, string Reason);

public sealed record OrderConfirmed(int OrderId,int SchoolId, string ParentEmail, decimal Total);
