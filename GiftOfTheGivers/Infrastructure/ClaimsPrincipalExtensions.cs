using System.Security.Claims;

namespace GiftOfTheGivers.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public const string DonorRole = "Donor";
    public const string EmployeeRole = "Employee";

    /// <summary>The signed-in user's id, or null when anonymous.</summary>
    public static string? UserId(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
}
