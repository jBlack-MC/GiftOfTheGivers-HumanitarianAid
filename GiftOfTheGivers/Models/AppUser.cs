using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GiftOfTheGivers.Models
{
    /// <summary>
    /// Application user. Extends the ASP.NET Identity user with the profile
    /// fields the Section B "Users" entity calls for (FullName, DateRegistered).
    /// The account's Donor / Employee role is held by ASP.NET Identity roles.
    /// </summary>
    public class AppUser : IdentityUser
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    }
}
