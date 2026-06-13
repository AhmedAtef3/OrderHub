namespace OrderHub.Contracts.Inventory;

public sealed record ReserveStockRequest(int OrderId, IReadOnlyList<ReservationLine> Lines);

public sealed record ReservationLine(string Sku, int Quantity);

public sealed record ReserveStockResponse(string ReservationId);

public sealed record StockConflict(string Sku, int Available);
