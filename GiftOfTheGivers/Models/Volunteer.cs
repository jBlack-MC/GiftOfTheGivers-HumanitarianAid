using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGivers.Models
{
    public class Volunteer
    {
        public int Id { get; set; }

        // Links a volunteer record to the account that owns it (nullable: a
        // volunteer can be registered from the public form without an account).
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Province { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Skills { get; set; } = string.Empty;

        [Required]
        public string Availability { get; set; } = string.Empty; // Weekdays, Weekends, Anytime

        public string? EmergencyContactName { get; set; }

        [Phone]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Active, Inactive

        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        public DateTime? ApprovalDate { get; set; }

        public string? Notes { get; set; }

        // Projects this volunteer is assigned to (many-to-many via VolunteerAssignment).
        public ICollection<VolunteerAssignment> Assignments { get; set; } = new List<VolunteerAssignment>();
    }
}
