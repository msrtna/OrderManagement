using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class StoreSettings : BaseEntity<int>
{
    public int ReservationDurationMinutes { get; set; } = 15;
    public int OrderExpirationHours { get; set; } = 24;
    public decimal DefaultShippingCost { get; set; } = 1000;
    public int MaxCartItemQuantity { get; set; } = 99;
}