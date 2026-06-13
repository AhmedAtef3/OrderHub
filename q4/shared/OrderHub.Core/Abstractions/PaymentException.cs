namespace OrderHub.Core.Abstractions;

public sealed class PaymentException(string message) : Exception(message);
