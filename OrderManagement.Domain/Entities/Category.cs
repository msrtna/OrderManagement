using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class Category : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
