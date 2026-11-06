using Microsoft.AspNetCore.Identity;

namespace OrderManagement.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public bool CanPurchase { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ShoppingCart? ShoppingCart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
}