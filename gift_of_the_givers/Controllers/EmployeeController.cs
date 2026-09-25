using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gift_of_the_givers.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly GiftOfTheGiversDbContext _context;

        public EmployeeController(
            GiftOfTheGiversDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // EMPLOYEE DASHBOARD
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var model = new EmployeeDashboardViewModel
            {
                TotalVolunteers = await _context.Volunteers.CountAsync(),

                PendingVolunteers = await _context.Volunteers
                    .CountAsync(v => v.Status == VolunteerStatus.Pending),

                ActiveVolunteers = await _context.Volunteers
                    .CountAsync(v => v.Status == VolunteerStatus.Active),

                TotalDonations = await _context.Donations
                    .CountAsync(),

                TotalDonationAmount = await _context.Donations
                    .SumAsync(d => (decimal?)d.Amount) ?? 0,

                TotalProjects = await _context.ReliefProjects
                    .CountAsync(),

                ActiveProjects = await _context.ReliefProjects
                    .CountAsync(p => p.Status == ReliefProjectStatus.Active),

                CompletedProjects = await _context.ReliefProjects
                    .CountAsync(p => p.Status == ReliefProjectStatus.Completed),

                RecentVolunteers = await _context.Volunteers
                    .OrderByDescending(v => v.RegistrationDate)
                    .Take(5)
                    .ToListAsync(),

                RecentDonations = await _context.Donations
                    .OrderByDescending(d => d.DonationDate)
                    .Take(5)
                    .ToListAsync(),

                ActiveReliefProjects = await _context.ReliefProjects
                    .Where(p => p.Status == ReliefProjectStatus.Active)
                    .OrderByDescending(p => p.StartDate)
                    .Take(5)
                    .ToListAsync(),

                RecentUpdates = await _context.ProjectUpdates
                    .Include(u => u.ReliefProject)
                    .Include(u => u.Employee)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }


        // =====================================================
        // VOLUNTEERS
        // =====================================================

        public async Task<IActionResult> Volunteers()
        {
            var volunteers = await _context.Volunteers
                .OrderByDescending(v => v.RegistrationDate)
                .ToListAsync();

            return View(volunteers);
        }


        // =====================================================
        // DONATIONS
        // =====================================================

        public async Task<IActionResult> Donations()
        {
            var donations = await _context.Donations
                .Include(d => d.User)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }


        // =====================================================
        // RELIEF PROJECTS
        // =====================================================

        public async Task<IActionResult> Projects()
        {
            var projects = await _context.ReliefProjects
                .Include(p => p.CreatedByUser)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }


        // =====================================================
        // PROJECT UPDATES
        // =====================================================

        public async Task<IActionResult> Updates()
        {
            var updates = await _context.ProjectUpdates
                .Include(u => u.ReliefProject)
                .Include(u => u.Employee)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(updates);
        }
    }
}