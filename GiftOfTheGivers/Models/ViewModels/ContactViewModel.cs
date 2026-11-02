using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models.ValidationAttributes;

namespace GiftOfTheGivers.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [MustBeTrue]
        [Display(Name = "I have read the privacy notice")]
        public bool PrivacyConsent { get; set; }
    }
}
