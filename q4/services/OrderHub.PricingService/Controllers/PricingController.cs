using Microsoft.AspNetCore.Mvc;
using OrderHub.Contracts.Inventory;
using OrderHub.Contracts.Pricing;
using OrderHub.PricingService.Services;

namespace OrderHub.PricingService.Controllers;

[ApiController]
[Route("price-order")]
public sealed class PricingController(PricingCalculatorService calculator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PriceOrder([FromBody] PriceOrderRequest request, CancellationToken ct)
    {
        var price = await calculator.PriceAsync(request, ct);
        if (price is null)
            return Conflict();
        return Ok(price);
    }
}
