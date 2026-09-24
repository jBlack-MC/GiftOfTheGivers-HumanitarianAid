using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;

namespace GiftOfTheGivers.Services;

public class AuditService(ApplicationDbContext context) : IAuditService
{
    public async Task LogAsync(string userId, string action, string targetEntity, int targetId)
    {
        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}