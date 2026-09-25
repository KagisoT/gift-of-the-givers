using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace app_class_library
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Volunteer profile
        public Volunteer? Volunteer { get; set; }

        // Donations made by this user
        public ICollection<Donation> Donations { get; set; }
            = new List<Donation>();

        // Relief projects created by employee
        public ICollection<ReliefProject> ReliefProjectsCreated { get; set; }
            = new List<ReliefProject>();

        // Project updates created by employee
        public ICollection<ProjectUpdate> ProjectUpdates { get; set; }
            = new List<ProjectUpdate>();
    }
}
