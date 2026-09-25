using System.Security.Claims;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gift_of_the_givers.Controllers
{
    public class DonationController : Controller
    {
        private readonly GiftOfTheGiversDbContext _context;

        public DonationController(GiftOfTheGiversDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // PUBLIC DONATION PAGE
        // GET: /Donation
        // ============================================================

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View(new Donation
            {
                DonationDate = DateTime.UtcNow
            });
        }

        // ============================================================
        // CREATE DONATION
        // GET: /Donation/Create
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            var donation = new Donation
            {
                DonationDate = DateTime.UtcNow,
                Currency = DonationCurrency.ZAR,
                DonationType = DonationType.OneTime,
                IsAnonymous = false
            };

            return View(donation);
        }

        // ============================================================
        // CREATE DONATION
        // POST: /Donation/Create
        // ============================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Donation donation)
        {
            // Never allow the browser to submit or manipulate UserId.
            donation.UserId = null;

            // The server controls the donation date.
            donation.DonationDate = DateTime.UtcNow;

            // Remove values that should not be submitted by the user.
            donation.TaxCertificateReference = null;

            if (!ModelState.IsValid)
            {
                return View(donation);
            }

            // Get currently logged-in user.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Attach donation to the logged-in user unless
            // the donor selected anonymous donation.
            if (!donation.IsAnonymous && !string.IsNullOrEmpty(userId))
            {
                donation.UserId = userId;
            }

            // Generate a placeholder tax certificate reference.
            donation.TaxCertificateReference =
                GenerateTaxCertificateReference();

            _context.Donations.Add(donation);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Confirmation),
                new { id = donation.DonationId });
        }

        // ============================================================
        // CONFIRMATION
        // GET: /Donation/Confirmation/5
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var donation = await _context.Donations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DonationId == id);

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // ============================================================
        // DONATION DETAILS
        // GET: /Donation/Details/5
        // ============================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var donation = await _context.Donations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DonationId == id);

            if (donation == null)
            {
                return NotFound();
            }

            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var isEmployee = User.IsInRole("Employee");

            // Donors can only view their own donations.
            // Employees can view all donations.
            if (!isEmployee && donation.UserId != currentUserId)
            {
                return Forbid();
            }

            return View(donation);
        }

        // ============================================================
        // MY DONATIONS
        // GET: /Donation/MyDonations
        // ============================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyDonations()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var donations = await _context.Donations
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        // ============================================================
        // EMPLOYEE DONATION MANAGEMENT
        // GET: /Donation/Manage
        // ============================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var donations = await _context.Donations
                .AsNoTracking()
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        // ============================================================
        // EMPLOYEE DELETE DONATION
        // GET: /Donation/Delete/5
        // ============================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var donation = await _context.Donations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DonationId == id);

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // ============================================================
        // EMPLOYEE DELETE DONATION
        // POST: /Donation/Delete/5
        // ============================================================

        [Authorize(Roles = "Employee")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donation = await _context.Donations
                .FirstOrDefaultAsync(d => d.DonationId == id);

            if (donation == null)
            {
                return NotFound();
            }

            _context.Donations.Remove(donation);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The donation has been removed.";

            return RedirectToAction(nameof(Manage));
        }

        // ============================================================
        // TAX CERTIFICATE PLACEHOLDER
        // ============================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TaxCertificate(int id)
        {
            var donation = await _context.Donations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DonationId == id);

            if (donation == null)
            {
                return NotFound();
            }

            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var isEmployee = User.IsInRole("Employee");

            if (!isEmployee && donation.UserId != currentUserId)
            {
                return Forbid();
            }

            return View(donation);
        }

        // ============================================================
        // PRIVATE METHOD
        // ============================================================

        private static string GenerateTaxCertificateReference()
        {
            return $"TAX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"
                .Substring(0, 25)
                .ToUpper();
        }
    }
}