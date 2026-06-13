using Dapper;
using Microsoft.Data.SqlClient;
using OrderHub.Contracts.Inventory;

namespace OrderHub.InventoryService.Repositories;

public sealed class InventoryRepository(IConfiguration config) : IInventoryRepository
{
    private SqlConnection Connect() => new(config.GetConnectionString("InventoryDb"));

    public async Task<int> GetAvailableStockAsync(string sku, CancellationToken ct)
    {
        await using var conn = Connect();

        return await conn.QuerySingleOrDefaultAsync<int>(new CommandDefinition(
            "SELECT Qty FROM Stock WHERE Sku = @Sku", new { Sku = sku }, cancellationToken: ct));
    }

    public async Task<string> CreateReservationAsync(int orderId, IReadOnlyList<ReservationLine> lines, CancellationToken ct)
    {
        var reservationId = Guid.NewGuid().ToString();
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO Reservations (Id, OrderId, CreatedUtc, ExpiresUtc, Status)
              VALUES (@Id, @OrderId, SYSUTCDATETIME(), DATEADD(MINUTE,15,SYSUTCDATETIME()), 'HELD')",
            new { Id = reservationId, OrderId = orderId }, cancellationToken: ct));
        return reservationId;
    }

    public async Task ReleaseReservationAsync(string reservationId, CancellationToken ct)
    {
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Reservations SET Status = 'RELEASED' WHERE Id = @Id",
            new { Id = reservationId }, cancellationToken: ct));
    }

    public async Task CommitReservationAsync(string reservationId, CancellationToken ct)
    {
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Reservations SET Status = 'COMMITTED' WHERE Id = @Id;
              UPDATE s SET s.Qty = s.Qty - rl.Quantity
              FROM Stock s
              JOIN ReservationLines rl ON rl.Sku = s.Sku
              WHERE rl.ReservationId = @Id",
            new { Id = reservationId }, cancellationToken: ct));
    }
}
