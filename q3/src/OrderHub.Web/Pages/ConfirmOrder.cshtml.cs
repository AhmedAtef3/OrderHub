using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrderHub.Application.Interfaces;
using OrderHub.Core.Models;

namespace OrderHub.Web.Pages;

[ValidateAntiForgeryToken]
public sealed class ConfirmOrderModel(IProcessOrderUseCase processOrder) : PageModel
{
    public string SchoolName { get; private set; } = string.Empty;

    public IReadOnlyList<OrderLineViewModel> Lines { get; private set; } = Array.Empty<OrderLineViewModel>();

    public decimal Subtotal => Lines.Sum(l => l.UnitPrice * l.Quantity);

    [BindProperty]
    public Dictionary<int, int> Quantities { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int schoolId, string parentEmail)
    {
        if (!ModelState.IsValid)
            return Page();

        var lines = Quantities
            .Select(kvp => new OrderLine(Sku: kvp.Key.ToString(), Quantity: kvp.Value))
            .ToList();

        var result = await processOrder.ExecuteAsync(schoolId, lines, parentEmail);

        return result switch
        {
            OrderResult.Success s => RedirectToPage("OrderConfirmed", new { reference = s.OrderReference, total = s.Total }),
            OrderResult.Failure f => BadRequest(f.Reason),
            _ => StatusCode(500)
        };
    }
}

public sealed record OrderLineViewModel(int Id, string Sku, string? Embroidery, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
