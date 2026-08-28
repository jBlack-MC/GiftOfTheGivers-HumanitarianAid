using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGivers.Models
{
    public class Donation
    {
        public int Id { get; set; }

        // Nullable so anonymous, non-logged-in donations are still possible.
        public string? DonorId { get; set; }

        [ForeignKey(nameof(DonorId))]
        public AppUser? Donor { get; set; }

        public int? ReliefProjectId { get; set; }

        [ForeignKey(nameof(ReliefProjectId))]
        public ReliefProject? ReliefProject { get; set; }

        [Required]
        [StringLength(20)]
        public string DonationType { get; set; } = "OneTime"; // OneTime or Recurring

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "ZAR"; // ZAR, USD or EUR

        [Required]
        [DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DonationDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Card"; // Card, EFT, Cash

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public bool TaxCertificateIssued { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
