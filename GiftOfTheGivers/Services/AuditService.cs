using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;

namespace GiftOfTheGivers.Services;

public class AuditService(ApplicationDbContext context) : IAuditService
{
    private const int MaxDetailsLength = 1000;

    public void Record(string userId, string action, string targetEntity, int targetId, string? details = null)
    {
        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Details = details is { Length: > MaxDetailsLength } ? details[..MaxDetailsLength] : details,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LogAsync(string userId, string action, string targetEntity, int targetId, string? details = null)
    {
        Record(userId, action, targetEntity, targetId, details);
        await context.SaveChangesAsync();
    }
}
