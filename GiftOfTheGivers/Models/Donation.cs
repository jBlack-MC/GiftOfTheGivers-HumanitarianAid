using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGivers.Models
{
    public class Donation
    {
        public int Id { get; set; }

        [Required]
        public string DonorId { get; set; } = string.Empty;

        [ForeignKey(nameof(DonorId))]
        public IdentityUser? Donor { get; set; }

        public int? ReliefProjectId { get; set; }

        [ForeignKey(nameof(ReliefProjectId))]
        public ReliefProject? ReliefProject { get; set; }

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
