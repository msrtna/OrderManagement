namespace OrderManagement.Domain.Entities
{
    public class StoreSettings
    {
        public int Id { get; set; }
        public int ReservationDurationMinutes { get; set; } = 15;
        public int OrderExpirationHours { get; set; } = 24;
        public decimal DefaultShippingCost { get; set; } = 1000;
    }
}
