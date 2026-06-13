namespace OrderHub.Core.Models;

public abstract record OrderResult
{
    public sealed record Success(string OrderReference, decimal Total) : OrderResult;
    public sealed record Failure(string Reason) : OrderResult;
}
