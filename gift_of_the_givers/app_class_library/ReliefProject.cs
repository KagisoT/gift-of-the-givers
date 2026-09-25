using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace app_class_library
{
    public class ReliefProject
    {
        [Key]
        public int ReliefProjectId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public ReliefProjectStatus Status { get; set; }
            = ReliefProjectStatus.Planned;

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
        public string? ImageUrl { get; set; }

        [Required]
        [ForeignKey(nameof(CreatedByUser))]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? CreatedByUser { get; set; }

        public ICollection<ProjectUpdate> ProjectUpdates { get; set; }
            = new List<ProjectUpdate>();

        public ICollection<VolunteerAssignment> VolunteerAssignments { get; set; }
            = new List<VolunteerAssignment>();
    }

    public enum ReliefProjectStatus
    {
        Planned,
        Active,
        Completed,
        Cancelled
    }
}
