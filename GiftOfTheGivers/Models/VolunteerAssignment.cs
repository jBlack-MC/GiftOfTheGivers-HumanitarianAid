using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGivers.Models
{
    /// <summary>
    /// Junction entity resolving the many-to-many relationship between
    /// volunteers and relief projects (Section B, entity 3.6).
    /// </summary>
    public class VolunteerAssignment
    {
        public int Id { get; set; }

        [Required]
        public int VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }

        [Required]
        public int ReliefProjectId { get; set; }

        [ForeignKey(nameof(ReliefProjectId))]
        public ReliefProject? ReliefProject { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
