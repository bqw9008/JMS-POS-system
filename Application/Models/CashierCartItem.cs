namespace POS_system_cs.Application.Models;

public sealed class CashierCartItem
{
    public Guid ProductId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal Quantity { get; set; } = 1;

    public decimal Amount => UnitPrice * Quantity;
}
