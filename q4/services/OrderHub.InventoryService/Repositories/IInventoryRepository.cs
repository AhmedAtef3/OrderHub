using OrderHub.Contracts.Inventory;

namespace OrderHub.InventoryService.Repositories;

public interface IInventoryRepository
{
    Task<int> GetAvailableStockAsync(string sku, CancellationToken ct);
    Task<string> CreateReservationAsync(int orderId, IReadOnlyList<ReservationLine> lines, CancellationToken ct);
    Task ReleaseReservationAsync(string reservationId, CancellationToken ct);
    Task CommitReservationAsync(string reservationId, CancellationToken ct);
}
