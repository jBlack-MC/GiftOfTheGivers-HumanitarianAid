using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGivers.Models
{
    public class ReliefProject
    {
        public int Id { get; set; }

        // The employee who created the project (Section B: ReliefProjects.CreatedByUserID).
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public AppUser? CreatedByUser { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        /// <summary>Funding goal in rand.</summary>
        [DataType(DataType.Currency)]
        public decimal FundsRequired { get; set; }

        /// <summary>
        /// Funds raised outside this system (cash, grants, corporate pledges), in rand. Maintained
        /// by employees and included in <see cref="FundsRaised"/>.
        /// </summary>
        [DataType(DataType.Currency)]
        public decimal OfflineFundsZar { get; set; }

        /// <summary>
        /// Total raised in rand = <see cref="OfflineFundsZar"/> + the ZAR value of every
        /// <see cref="PaymentStatus.Verified"/> donation to this project. It is maintained
        /// server-side only (payment verification and the offline-funds edit) and must never be
        /// bound from a form. <c>FundingReconciliation</c> recomputes it from the source data.
        /// </summary>
        [DataType(DataType.Currency)]
        public decimal FundsRaised { get; set; }

        public string? ImageUrl { get; set; }

        /// <summary>
        /// SQL Server <c>rowversion</c> concurrency token. Every update of a project (including the
        /// FundsRaised increment made when a payment is verified) must match the value that was
        /// read, so concurrent updates fail fast with a
        /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> and are retried
        /// instead of silently lost-updating each other.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();
        public ICollection<VolunteerAssignment> VolunteerAssignments { get; set; } = new List<VolunteerAssignment>();
    }
}
