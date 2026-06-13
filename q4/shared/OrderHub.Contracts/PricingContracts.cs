namespace OrderHub.Contracts.Pricing;

public sealed record PriceOrderRequest(int SchoolId, IReadOnlyList<PriceOrderLine> Lines);

public sealed record PriceOrderLine(string Sku, int Quantity, string? Embroidery);

public sealed record PriceOrderResponse(IReadOnlyList<PricedLine> PricedLines, decimal Subtotal);

public sealed record PricedLine(string  Sku, decimal UnitPrice, decimal LineTotal);
