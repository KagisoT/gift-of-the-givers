using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace app_class_library
{
    public class VolunteerAssignment
    {
        [Key]
        public int VolunteerAssignmentId { get; set; }

        [Required]
        [ForeignKey(nameof(Volunteer))]
        public int VolunteerId { get; set; }

        [Required]
        [ForeignKey(nameof(ReliefProject))]
        public int ReliefProjectId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string Role { get; set; } = string.Empty;

        [Required]
        public AssignmentStatus Status { get; set; }
            = AssignmentStatus.Assigned;

        // Navigation properties
        public Volunteer? Volunteer { get; set; }

        public ReliefProject? ReliefProject { get; set; }
    }

    public enum AssignmentStatus
    {
        Assigned,
        Active,
        Completed,
        Cancelled
    }
}
