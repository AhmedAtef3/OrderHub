namespace OrderHub.Core.Models;

public sealed record OrderLine(string Sku, int Quantity, string? Embroidery = null);
