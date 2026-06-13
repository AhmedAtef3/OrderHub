using Microsoft.AspNetCore.Mvc;
using OrderHub.Contracts.Inventory;
using OrderHub.InventoryService.Repositories;

namespace OrderHub.InventoryService.Controllers;

[ApiController]
[Route("reserve")]
public sealed class InventoryController(IInventoryRepository repo) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Reserve([FromBody] ReserveStockRequest request, CancellationToken ct)
    {
        foreach (var line in request.Lines)
        {
            var available = await repo.GetAvailableStockAsync(line.Sku, ct);
            if (available < line.Quantity)
                return Conflict(new StockConflict(line.Sku, available));
        }

        var reservationId = await repo.CreateReservationAsync(request.OrderId, request.Lines, ct);
        return Ok(new ReserveStockResponse(reservationId));
    }

    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> Release(string reservationId, CancellationToken ct)
    {
        await repo.ReleaseReservationAsync(reservationId, ct);
        return NoContent();
    }

    [HttpPatch("{reservationId}/commit")]
    public async Task<IActionResult> Commit(string reservationId, CancellationToken ct)
    {
        await repo.CommitReservationAsync(reservationId, ct);
        return NoContent();
    }
}
