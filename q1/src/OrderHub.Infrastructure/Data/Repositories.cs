using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OrderHub.Core.Abstractions;
using OrderHub.Core.Models;

namespace OrderHub.Infrastructure.Data;

public sealed record DatabaseOptions(string ConnectionString);

public sealed class SchoolRepository(IOptions<DatabaseOptions> opts) : ISchoolRepository
{
    public async Task<PricingTier?> GetPricingTierAsync(int schoolId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(opts.Value.ConnectionString);

        var tierCode = await conn.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT TierCode FROM Schools WHERE Id = @SchoolId", new { SchoolId = schoolId }, cancellationToken: ct));

        return tierCode?.ToUpper() switch
        {
            "GOLD" => PricingTier.Gold,
            "SILVER" => PricingTier.Silver,
            null => null,
            _ => PricingTier.Standard
        };
    }
}

public sealed class ProductRepository(IOptions<DatabaseOptions> opts) : IProductRepository
{
    public async Task<decimal?> GetBasePriceAsync(string sku, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(opts.Value.ConnectionString);

        return await conn.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
            "SELECT BasePrice FROM Products WHERE Sku = @Sku", new { Sku = sku }, cancellationToken: ct));
    }
}

public sealed class StockRepository(IOptions<DatabaseOptions> opts) : IStockRepository
{
    public async Task<int> GetAvailableStockAsync(string sku, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(opts.Value.ConnectionString);

        return await conn.QuerySingleOrDefaultAsync<int>(new CommandDefinition(
            "SELECT Qty FROM Stock WHERE Sku = @Sku", new { Sku = sku }, cancellationToken: ct));
    }
}
