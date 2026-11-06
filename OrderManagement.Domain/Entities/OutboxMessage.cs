using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Entities;

public class OutboxMessage : BaseEntity<long>
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsFailed { get; set; }
}