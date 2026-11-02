using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models.ValidationAttributes;

namespace GiftOfTheGivers.Models.ViewModels
{
    public class VolunteerApplicationViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number (optional)")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Province { get; set; }

        [StringLength(10)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth (optional)")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [StringLength(2000)]
        [Display(Name = "Skills")]
        public string Skills { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Availability")]
        public string Availability { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Emergency Contact Phone")]
        public string? EmergencyContactPhone { get; set; }

        [MustBeTrue]
        [Display(Name = "I have read the privacy notice")]
        public bool PrivacyConsent { get; set; }
    }
}
