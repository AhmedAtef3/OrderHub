using Azure;
using OrderHub.Contracts.Events;
using OrderHub.Contracts.Inventory;
using OrderHub.Contracts.Orders;
using OrderHub.Contracts.Payment;
using OrderHub.Contracts.Pricing;
using OrderHub.OrderService.Broker;
using OrderHub.OrderService.HttpClients;
using OrderHub.OrderService.Repositories;

namespace OrderHub.OrderService.Saga;

public sealed class OrderSaga(IPricingClient pricing, IInventoryClient inventory, IPaymentClient payment,
    IOrderRepository repo, IBrokerPublisher broker)
{
    public async Task<int> StartAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var orderId = await repo.CreateDraftAsync(request, ct);

        var priceRequest = new PriceOrderRequest(request.SchoolId,
            request.Lines.Select(l => new PriceOrderLine(l.Sku, l.Quantity, l.Embroidery)).ToList());
        var priced = await pricing.PriceOrderAsync(priceRequest, ct);
        await repo.UpdateStatusAsync(orderId, "pricing", ct);

        var reserveRequest = new ReserveStockRequest(orderId,
            request.Lines.Select(l => new ReservationLine(l.Sku, l.Quantity)).ToList());
        var reservation = await inventory.ReserveAsync(reserveRequest, ct);
        await repo.SetReservationIdAsync(orderId, reservation.ReservationId, ct);
        await repo.UpdateStatusAsync(orderId, "reserved", ct);

        var intentRequest = new CreateIntentRequest(priced.Subtotal, request.ParentEmail, 
            orderId, orderId.ToString());
        var intent = await payment.CreateIntentAsync(intentRequest, ct);
        await repo.SetPaymentIntentIdAsync(orderId, intent.IntentId, ct);
        await repo.UpdateStatusAsync(orderId, "payment_pending", ct);

        return orderId;
    }

    public async Task HandlePaymentResultAsync(int orderId, string status, CancellationToken ct)
    {
        var order = await repo.GetStatusAsync(orderId, ct) ?? throw new InvalidOperationException($"Order {orderId} not found");

        if (status == "succeeded")
        {
            await inventory.CommitAsync(order.ReservationId!, ct);
            await repo.UpdateStatusAsync(orderId, "confirmed", ct);
            await broker.PublishAsync(new OrderConfirmed(orderId, order.SchoolId, order.ParentEmail, order.Total!.Value), ct);
        }
        else
        {
            await inventory.ReleaseAsync(order.ReservationId!, ct);
            await repo.UpdateStatusAsync(orderId, "cancelled", ct);
        }
    }
}
