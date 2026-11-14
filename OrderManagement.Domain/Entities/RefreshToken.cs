using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class RefreshToken : BaseEntity<long>
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
