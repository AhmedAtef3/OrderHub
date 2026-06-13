using Dapper;
using Microsoft.Data.SqlClient;

namespace OrderHub.PaymentService.Repositories;

public sealed class SqlPaymentRepository(IConfiguration config) : IPaymentRepository
{
    private SqlConnection Connect() => new(config.GetConnectionString("PaymentDb"));

    public async Task SaveIntentAsync(string intentId, int orderId, decimal amount, CancellationToken ct)
    {
        await using var conn = Connect();
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO PaymentIntents (IntentId, OrderId, Amount, Status, CreatedUtc)
              VALUES (@IntentId, @OrderId, @Amount, 'pending', SYSUTCDATETIME())",
            new { IntentId = intentId, OrderId = orderId, Amount = amount },
            cancellationToken: ct));
    }

    public async Task<string?> GetIntentByOrderIdAsync(int orderId, CancellationToken ct)
    {
        await using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition("SELECT IntentId FROM PaymentIntents WHERE OrderId = @OrderId",
                new { OrderId = orderId }, cancellationToken: ct));
    }

    public async Task<int?> GetOrderIdByIntentAsync(string intentId, CancellationToken ct)
    {
        await using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition("SELECT OrderId FROM PaymentIntents WHERE IntentId = @IntentId",
                new { IntentId = intentId }, cancellationToken: ct));
    }
}
