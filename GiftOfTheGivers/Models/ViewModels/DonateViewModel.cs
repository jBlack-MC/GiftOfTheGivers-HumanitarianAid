using System.ComponentModel.DataAnnotations;
using GiftOfTheGivers.Models.ValidationAttributes;

namespace GiftOfTheGivers.Models.ViewModels
{
    public class DonateViewModel
    {
        /// <summary>Largest single donation accepted online, in the chosen currency.</summary>
        public const int MaximumAmount = 1_000_000;

        public Guid IdempotencyToken { get; set; } = Guid.NewGuid();

        [Display(Name = "Project")]
        public int? ReliefProjectId { get; set; }
        public string? ProjectTitle { get; set; }

        [Required]
        [AllowedValues("OneTime", "Recurring", ErrorMessage = "Choose a one-time or monthly donation.")]
        [Display(Name = "Donation Type")]
        public string DonationType { get; set; } = "OneTime";

        [Required]
        [AllowedValues("ZAR", "USD", "EUR", ErrorMessage = "Choose ZAR, USD or EUR.")]
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "ZAR";

        [Required]
        [DataType(DataType.Currency)]
        [Range(typeof(decimal), "1", "1000000",
            ErrorMessage = "Amount must be between 1 and 1,000,000.")]
        [Display(Name = "Donation Amount")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Your Name")]
        public string DonorName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Your Email")]
        public string DonorEmail { get; set; } = string.Empty;

        [Required]
        [AllowedValues("Card", "EFT", ErrorMessage = "Choose card or EFT.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Card";

        [Display(Name = "Make donation anonymous")]
        public bool IsAnonymous { get; set; }

        [StringLength(500)]
        [Display(Name = "Message (optional)")]
        public string? Notes { get; set; }

        [MustBeTrue]
        [Display(Name = "I have read the privacy notice")]
        public bool PrivacyConsent { get; set; }

        // Populated by the controller for display only; never bound from the form.
        public List<ReliefProject>? AvailableProjects { get; set; }
        public IReadOnlyDictionary<string, decimal> RatesToZar { get; set; } = new Dictionary<string, decimal>();
        public bool PaymentsEnabled { get; set; }
        public bool PaymentsSandbox { get; set; }
    }
}
