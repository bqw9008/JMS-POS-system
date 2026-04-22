namespace POS_system_cs.Domain.Entities;

public sealed class StockItem : EntityBase
{
    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime LastChangedAt { get; set; } = DateTime.Now;
}
