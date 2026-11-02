namespace GiftOfTheGivers.Services;

public interface IAuditService
{
    /// <summary>
    /// Stages an audit entry on the shared DbContext without saving, so it commits in the same
    /// SaveChanges call (and therefore the same transaction) as the change it describes.
    /// </summary>
    void Record(string userId, string action, string targetEntity, int targetId, string? details = null);

    /// <summary>Records and saves immediately. Use for read events that change nothing else.</summary>
    Task LogAsync(string userId, string action, string targetEntity, int targetId, string? details = null);
}
