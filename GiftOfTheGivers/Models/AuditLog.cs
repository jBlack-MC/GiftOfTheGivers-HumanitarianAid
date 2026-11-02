using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models;

public class AuditLog
{
    /// <summary>Actor id used for actions taken by the payment provider rather than a user.</summary>
    public const string SystemPayFast = "system:payfast";

    public int Id { get; set; }

    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(100)]
    public string TargetEntity { get; set; } = string.Empty;

    public int TargetId { get; set; }

    /// <summary>Short human-readable description of what changed.</summary>
    [StringLength(1000)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
