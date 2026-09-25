using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace app_class_library
{
    public class Volunteer
    {
        [Key]
        public int VolunteerId { get; set; }

        [ForeignKey(nameof(User))]
        public string? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Skills { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Availability { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public VolunteerStatus Status { get; set; }
            = VolunteerStatus.Pending;

        // Navigation properties
        public ApplicationUser? User { get; set; }

        public ICollection<VolunteerAssignment> Assignments { get; set; }
            = new List<VolunteerAssignment>();
    }
    public enum VolunteerStatus
    {
        Pending,
        Approved,
        Active,
        Inactive
    }
}
