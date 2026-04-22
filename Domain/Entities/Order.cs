using POS_system_cs.Domain.Enums;

namespace POS_system_cs.Domain.Entities;

public sealed class Order : EntityBase
{
    public string OrderNo { get; set; } = string.Empty;

    public DateTime OrderedAt { get; set; } = DateTime.Now;

    public Guid? OperatorId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ReceivedAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public List<OrderItem> Items { get; } = [];

    public decimal ChangeAmount => ReceivedAmount - (TotalAmount - DiscountAmount);
}
