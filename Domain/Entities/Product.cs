namespace POS_system_cs.Domain.Entities;

public sealed class Product : EntityBase
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal LowStockThreshold { get; set; }

    public bool IsActive { get; set; } = true;
}
