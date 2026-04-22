namespace POS_system_cs.Domain.Entities;

public sealed class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount => Quantity * UnitPrice;
}
