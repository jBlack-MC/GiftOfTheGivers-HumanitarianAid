namespace GiftOfTheGivers.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string targetEntity, int targetId);
}