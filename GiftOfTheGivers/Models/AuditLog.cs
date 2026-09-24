namespace GiftOfTheGivers.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}