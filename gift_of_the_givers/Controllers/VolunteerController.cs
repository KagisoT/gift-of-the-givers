using System.Security.Claims;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gift_of_the_givers.Controllers
{
    public class VolunteerController : Controller
    {
        private readonly GiftOfTheGiversDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public VolunteerController(
            GiftOfTheGiversDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ============================================================
        // GET: Volunteer
        // Public volunteer directory
        // ============================================================
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            VolunteerStatus? status)
        {
            var query = _context.Volunteers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(v =>
                    v.FirstName.Contains(search) ||
                    v.LastName.Contains(search) ||
                    v.Email.Contains(search) ||
                    v.Skills.Contains(search) ||
                    v.Availability.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(v => v.Status == status.Value);
            }

            var volunteers = await query
                .OrderByDescending(v => v.RegistrationDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(volunteers);
        }

        // ============================================================
        // GET: Volunteer/Details/5
        // ============================================================
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var volunteer = await _context.Volunteers
                .Include(v => v.Assignments)
                    .ThenInclude(a => a.ReliefProject)
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            return View(volunteer);
        }

        // ============================================================
        // GET: Volunteer/Create
        // Public registration
        // ============================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Volunteer());
        }

        // ============================================================
        // POST: Volunteer/Create
        // ============================================================
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Volunteer volunteer,
            IFormFile? imageFile)
        {
            // These fields are controlled by the application.
            ModelState.Remove(nameof(volunteer.UserId));
            ModelState.Remove(nameof(volunteer.User));
            ModelState.Remove(nameof(volunteer.Assignments));
            ModelState.Remove(nameof(volunteer.RegistrationDate));
            ModelState.Remove(nameof(volunteer.Status));
            ModelState.Remove(nameof(volunteer.ImageUrl));

            if (!ModelState.IsValid)
            {
                return View(volunteer);
            }

            // --------------------------------------------------------
            // Prevent duplicate volunteer email registrations
            // --------------------------------------------------------
            var existingVolunteer = await _context.Volunteers
                .AnyAsync(v => v.Email == volunteer.Email);

            if (existingVolunteer)
            {
                ModelState.AddModelError(
                    nameof(volunteer.Email),
                    "A volunteer with this email address already exists.");

                return View(volunteer);
            }

            // --------------------------------------------------------
            // Upload image
            // --------------------------------------------------------
            if (imageFile != null && imageFile.Length > 0)
            {
                var imageUrl = await SaveImageAsync(imageFile);

                if (imageUrl == null)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "The selected image is invalid. Please upload a JPG, JPEG, PNG or WEBP image up to 5 MB.");

                    return View(volunteer);
                }

                volunteer.ImageUrl = imageUrl;
            }

            // --------------------------------------------------------
            // Automatically assign registration values
            // --------------------------------------------------------
            volunteer.RegistrationDate = DateTime.UtcNow;
            volunteer.Status = VolunteerStatus.Pending;

            // If the user is logged in, connect the volunteer
            // record to their Identity account.
            if (User.Identity?.IsAuthenticated == true)
            {
                volunteer.UserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            _context.Volunteers.Add(volunteer);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your volunteer application has been submitted successfully. " +
                "Our team will review your application.";

            return RedirectToAction(nameof(Details),
                new { id = volunteer.VolunteerId });
        }

        // ============================================================
        // GET: Volunteer/Edit/5
        // Employee only
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            return View(volunteer);
        }

        // ============================================================
        // POST: Volunteer/Edit/5
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Volunteer volunteer,
            IFormFile? imageFile)
        {
            if (id != volunteer.VolunteerId)
                return NotFound();

            ModelState.Remove(nameof(volunteer.UserId));
            ModelState.Remove(nameof(volunteer.User));
            ModelState.Remove(nameof(volunteer.Assignments));
            ModelState.Remove(nameof(volunteer.RegistrationDate));
            ModelState.Remove(nameof(volunteer.Status));
            ModelState.Remove(nameof(volunteer.ImageUrl));

            if (!ModelState.IsValid)
            {
                return View(volunteer);
            }

            var existingVolunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (existingVolunteer == null)
                return NotFound();

            // --------------------------------------------------------
            // Check duplicate email
            // --------------------------------------------------------
            var duplicateEmail = await _context.Volunteers
                .AnyAsync(v =>
                    v.VolunteerId != id &&
                    v.Email == volunteer.Email);

            if (duplicateEmail)
            {
                ModelState.AddModelError(
                    nameof(volunteer.Email),
                    "Another volunteer already uses this email address.");

                return View(volunteer);
            }

            // --------------------------------------------------------
            // Store old image
            // --------------------------------------------------------
            var oldImage = existingVolunteer.ImageUrl;

            // --------------------------------------------------------
            // Update normal fields
            // --------------------------------------------------------
            existingVolunteer.FirstName = volunteer.FirstName;
            existingVolunteer.LastName = volunteer.LastName;
            existingVolunteer.Email = volunteer.Email;
            existingVolunteer.PhoneNumber = volunteer.PhoneNumber;
            existingVolunteer.Skills = volunteer.Skills;
            existingVolunteer.Availability = volunteer.Availability;

            // --------------------------------------------------------
            // Upload replacement image
            // --------------------------------------------------------
            if (imageFile != null && imageFile.Length > 0)
            {
                var newImageUrl = await SaveImageAsync(imageFile);

                if (newImageUrl == null)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "The selected image is invalid. Please upload a JPG, JPEG, PNG or WEBP image up to 5 MB.");

                    return View(volunteer);
                }

                existingVolunteer.ImageUrl = newImageUrl;

                DeleteImage(oldImage);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer information updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // GET: Volunteer/Delete/5
        // Employee only
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var volunteer = await _context.Volunteers
                .Include(v => v.Assignments)
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            return View(volunteer);
        }

        // ============================================================
        // POST: Volunteer/Delete/5
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            var imageUrl = volunteer.ImageUrl;

            _context.Volunteers.Remove(volunteer);

            await _context.SaveChangesAsync();

            DeleteImage(imageUrl);

            TempData["SuccessMessage"] =
                "Volunteer has been removed successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // POST: Volunteer/Approve/5
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            volunteer.Status = VolunteerStatus.Approved;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer application approved.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // POST: Volunteer/Activate/5
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            volunteer.Status = VolunteerStatus.Active;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer has been activated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // POST: Volunteer/Deactivate/5
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            volunteer.Status = VolunteerStatus.Inactive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer has been marked as inactive.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // POST: Volunteer/Reject/5
        // ============================================================
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            volunteer.Status = VolunteerStatus.Inactive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer application has been marked as inactive.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // Save image
        // ============================================================
        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            const long maxFileSize = 5 * 1024 * 1024;

            if (imageFile.Length > maxFileSize)
                return null;

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var extension =
                Path.GetExtension(imageFile.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return null;

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "volunteers");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(uploadsFolder, fileName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);

            await imageFile.CopyToAsync(stream);

            return $"/uploads/volunteers/{fileName}";
        }

        // ============================================================
        // Delete image
        // ============================================================
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            var relativePath =
                imageUrl.TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            var fullPath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch
                {
                    // Do not interrupt the request if image deletion
                    // fails after the database operation succeeded.
                }
            }
        }
    }
}