using Dapper;
using Microsoft.Data.SqlClient;
using OrderHub.Core.Models;

namespace OrderHub.PricingService.Repositories;

public sealed class PricingRepository(IConfiguration config) : IPricingRepository
{
    private SqlConnection Connect() => new(config.GetConnectionString("PricingDb"));

    public async Task<PricingTier?> GetPricingTierAsync(int schoolId, CancellationToken ct)
    {
        await using var conn = Connect();

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

    public async Task<decimal?> GetBasePriceAsync(string sku, CancellationToken ct)
    {
        await using var conn = Connect();

        return await conn.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
            "SELECT BasePrice FROM Products WHERE Sku = @Sku", new { Sku = sku }, cancellationToken: ct));
    }
}
