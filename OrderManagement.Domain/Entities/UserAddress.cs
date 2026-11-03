using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities
{
    public class UserAddress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Title Title { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public long PhoneNumber { get; set; }
        public long PostalCode { get; set; }
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
