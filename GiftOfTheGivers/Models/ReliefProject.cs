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

        [DataType(DataType.Currency)]
        public decimal FundsRequired { get; set; }

        // FundsRaised is derived from the Donations that point at this project and is
        // maintained server-side only (see HomeController.Donate). It must never be
        // bound from a form: the Create/Edit project views no longer render it.
        [DataType(DataType.Currency)]
        public decimal FundsRaised { get; set; }

        public string? ImageUrl { get; set; }

        /// <summary>
        /// SQL Server <c>rowversion</c> concurrency token. Every update of a project
        /// (including the FundsRaised increment made by a donation) must match the value
        /// that was read, so two concurrent donations against the same project fail fast
        /// with a <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>
        /// instead of silently lost-updating each other.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();
        public ICollection<VolunteerAssignment> VolunteerAssignments { get; set; } = new List<VolunteerAssignment>();
    }
}
