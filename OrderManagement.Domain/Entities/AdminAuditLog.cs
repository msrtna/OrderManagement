using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class AdminAuditLog : BaseEntity<long>
{
    public string AdminUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}