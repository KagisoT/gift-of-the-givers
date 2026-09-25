using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace app_class_library
{
    public class ProjectUpdate
    {
        [Key]
        public int ProjectUpdateId { get; set; }

        [Required]
        [ForeignKey(nameof(ReliefProject))]
        public int ReliefProjectId { get; set; }

        [Required]
        [ForeignKey(nameof(Employee))]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ReliefProject? ReliefProject { get; set; }

        public ApplicationUser? Employee { get; set; }
    }
}
