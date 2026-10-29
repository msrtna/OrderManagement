namespace OrderManagement.Domain.Entities
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        //public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
