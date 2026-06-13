using Dapper;
using Microsoft.Data.SqlClient;
using OrderHub.Contracts.Orders;

namespace OrderHub.OrderService.Repositories;

public sealed class OrderRepository(IConfiguration config) : IOrderRepository
{
    private SqlConnection Connect() => new(config.GetConnectionString("OrderDb"));

    public async Task<int> CreateDraftAsync(CreateOrderRequest request, CancellationToken ct)
    {
        await using var conn = Connect();
        return await conn.QuerySingleAsync<int>(new CommandDefinition(
            @"INSERT INTO Orders (SchoolId, ParentEmail, Status, CreatedUtc)
            VALUES (@SchoolId, @ParentEmail, 'draft', SYSUTCDATETIME());
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new { request.SchoolId, request.ParentEmail }, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(int orderId, string status, CancellationToken ct)
    {
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition("UPDATE Orders SET Status = @Status WHERE Id = @Id",
            new { Status = status, Id = orderId }, cancellationToken: ct));
    }

    public async Task SetReservationIdAsync(int orderId, string reservationId, CancellationToken ct)
    {
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Orders SET ReservationId = @ReservationId WHERE Id = @Id",
            new { ReservationId = reservationId, Id = orderId }, cancellationToken: ct));
    }

    public async Task SetPaymentIntentIdAsync(int orderId, string intentId, CancellationToken ct)
    {
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Orders SET PaymentIntentId = @IntentId WHERE Id = @Id",
            new { IntentId = intentId, Id = orderId }, cancellationToken: ct));
    }

    public async Task<OrderSagaState?> GetStatusAsync(int orderId, CancellationToken ct)
    {
        await using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<OrderSagaState>(new CommandDefinition(
            @"SELECT Id AS OrderId, SchoolId, ParentEmail, Status, ReservationId, PaymentIntentId, Total
              FROM Orders WHERE Id = @Id",
            new { Id = orderId }, cancellationToken: ct));
    }
}
