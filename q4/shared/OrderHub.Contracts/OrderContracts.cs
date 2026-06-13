namespace OrderHub.Contracts.Orders;

public sealed record CreateOrderRequest(int SchoolId, string ParentEmail, IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderLineRequest(string Sku, int Quantity, string? Embroidery);

public sealed record CreateOrderResponse(int OrderId, string Status);

public sealed record OrderStatusResponse(int OrderId, string Status, decimal? Total);
