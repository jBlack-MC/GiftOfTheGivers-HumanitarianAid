using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGivers.Models
{
    public class Donation
    {
        public static readonly string[] SupportedCurrencies = ["ZAR", "USD", "EUR"];
        public static readonly string[] SupportedDonationTypes = ["OneTime", "Recurring"];
        public static readonly string[] SupportedPaymentMethods = ["Card", "EFT"];

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

        /// <summary>Amount in the currency the donor chose.</summary>
        [Required]
        [DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Amount converted to rand at capture time. PayFast only settles in ZAR, so this is
        /// the figure that is charged and the only figure ever added to project totals.
        /// </summary>
        public decimal AmountZar { get; set; }

        /// <summary>Rate used to compute <see cref="AmountZar"/> (1 for ZAR donations).</summary>
        public decimal ExchangeRateToZar { get; set; } = 1m;

        [DataType(DataType.DateTime)]
        public DateTime DonationDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Card"; // Card or EFT

        /// <summary>
        /// Settlement state. A donation is captured as <see cref="PaymentStatus.Pending"/> and only
        /// becomes <see cref="PaymentStatus.Verified"/> when a validated PayFast ITN confirms it.
        /// Only verified donations count toward project totals or receive a tax certificate.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        /// <summary>When the payment provider confirmed the payment (UTC).</summary>
        public DateTime? PaymentVerifiedAt { get; set; }

        /// <summary>The provider's own payment id (PayFast <c>pf_payment_id</c>).</summary>
        [StringLength(50)]
        public string? ProviderPaymentId { get; set; }

        public Guid IdempotencyKey { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Unguessable public reference (32 hex chars). Sent to PayFast as <c>m_payment_id</c> and
        /// used in receipt links instead of the sequential database id.
        /// </summary>
        [StringLength(100)]
        public string? TransactionReference { get; set; }

        [StringLength(100)]
        public string? DonorName { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? DonorEmail { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public bool TaxCertificateIssued { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>When the donor accepted the privacy notice (UTC). Null for legacy rows.</summary>
        public DateTime? PrivacyConsentAt { get; set; }
    }
}
