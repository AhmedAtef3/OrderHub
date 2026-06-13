using Microsoft.AspNetCore.Mvc;
using OrderHub.Contracts.Orders;
using OrderHub.OrderService.Repositories;
using OrderHub.OrderService.Saga;

namespace OrderHub.OrderService.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderSaga saga, IOrderRepository repo) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var orderId = await saga.StartAsync(request, ct);
        return Accepted(new CreateOrderResponse(orderId, "draft"));
    }

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetOrder(int orderId, CancellationToken ct)
    {
        var order = await repo.GetStatusAsync(orderId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPatch("{orderId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] string status, CancellationToken ct)
    {
        await saga.HandlePaymentResultAsync(orderId, status, ct);
        return NoContent();
    }
}
