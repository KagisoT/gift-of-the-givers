using gift_of_the_givers.Models;

namespace gift_of_the_givers.Models
{
    public class EmployeeDashboardViewModel
    {
        public int TotalVolunteers { get; set; }

        public int PendingVolunteers { get; set; }

        public int ActiveVolunteers { get; set; }

        public int TotalDonations { get; set; }

        public decimal TotalDonationAmount { get; set; }

        public int ActiveProjects { get; set; }

        public int CompletedProjects { get; set; }

        public int TotalProjects { get; set; }

        public List<Volunteer> RecentVolunteers { get; set; }
            = new();

        public List<Donation> RecentDonations { get; set; }
            = new();

        public List<ReliefProject> ActiveReliefProjects { get; set; }
            = new();

        public List<ProjectUpdate> RecentUpdates { get; set; }
            = new();
    }
}