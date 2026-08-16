using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGivers.Models.ViewModels
{
    public class DonateViewModel
    {
        public int? ReliefProjectId { get; set; }
        public string? ProjectTitle { get; set; }

        [Required]
        [Display(Name = "Donation Type")]
        public string DonationType { get; set; } = "OneTime"; // OneTime or Recurring

        [Required]
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "ZAR"; // ZAR, USD, or EUR

        [Required]
        [DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [Display(Name = "Donation Amount")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Your Name")]
        public string DonorName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Your Email")]
        public string DonorEmail { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Card";

        [Display(Name = "Make donation anonymous")]
        public bool IsAnonymous { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes (Optional)")]
        public string? Notes { get; set; }

        // List of all projects for selection
        public List<ReliefProject>? AvailableProjects { get; set; }
    }
}

