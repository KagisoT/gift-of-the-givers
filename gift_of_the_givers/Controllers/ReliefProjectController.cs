using System.Security.Claims;
using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gift_of_the_givers.Controllers
{
    public class ReliefProjectController : Controller
    {
        private readonly GiftOfTheGiversDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReliefProjectController(
            GiftOfTheGiversDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =========================================================
        // INDEX
        // Publicly accessible list of relief projects
        // =========================================================

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            ReliefProjectStatus? status)
        {
            var query = _context.ReliefProjects
                .Include(p => p.CreatedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Location.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(projects);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.ReliefProjects
                .Include(p => p.CreatedByUser)
                .Include(p => p.ProjectUpdates)
                    .ThenInclude(u => u.Employee)
                .Include(p => p.VolunteerAssignments)
                    .ThenInclude(a => a.Volunteer)
                .FirstOrDefaultAsync(p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ReliefProject project,
            IFormFile? imageFile)
        {
            // CreatedBy is assigned automatically.
            ModelState.Remove(nameof(project.CreatedBy));
            ModelState.Remove(nameof(project.CreatedByUser));

            if (!ModelState.IsValid)
            {
                return View(project);
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            project.CreatedBy = userId;
            project.CreatedAt = DateTime.UtcNow;

            // Upload image
            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var imageUrl = await SaveImageAsync(imageFile);

                if (imageUrl == null)
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        "The image could not be uploaded.");

                    return View(project);
                }

                project.ImageUrl = imageUrl;
            }

            _context.ReliefProjects.Add(project);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Relief project created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.ReliefProjects
                .FirstOrDefaultAsync(
                    p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ReliefProject project,
            IFormFile? imageFile)
        {
            if (id != project.ReliefProjectId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(project.CreatedBy));
            ModelState.Remove(nameof(project.CreatedByUser));

            if (!ModelState.IsValid)
            {
                return View(project);
            }

            var existingProject =
                await _context.ReliefProjects
                    .FirstOrDefaultAsync(
                        p => p.ReliefProjectId == id);

            if (existingProject == null)
            {
                return NotFound();
            }

            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.Location = project.Location;
            existingProject.Status = project.Status;
            existingProject.StartDate = project.StartDate;
            existingProject.EndDate = project.EndDate;

            // Upload replacement image
            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var newImageUrl =
                    await SaveImageAsync(imageFile);

                if (newImageUrl == null)
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        "The image could not be uploaded.");

                    return View(project);
                }

                // Delete old image
                DeleteImage(existingProject.ImageUrl);

                existingProject.ImageUrl = newImageUrl;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(project.ReliefProjectId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["SuccessMessage"] =
                "Relief project updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = project.ReliefProjectId });
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.ReliefProjects
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(
                    p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var project =
                await _context.ReliefProjects
                    .FirstOrDefaultAsync(
                        p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            DeleteImage(project.ImageUrl);

            _context.ReliefProjects.Remove(project);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Relief project deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MARK PROJECT AS ACTIVE
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var project =
                await _context.ReliefProjects
                    .FirstOrDefaultAsync(
                        p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            project.Status = ReliefProjectStatus.Active;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Project has been activated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // MARK PROJECT AS COMPLETED
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var project =
                await _context.ReliefProjects
                    .FirstOrDefaultAsync(
                        p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            project.Status = ReliefProjectStatus.Completed;

            if (!project.EndDate.HasValue)
            {
                project.EndDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Project has been marked as completed.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // CANCEL PROJECT
        // =========================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var project =
                await _context.ReliefProjects
                    .FirstOrDefaultAsync(
                        p => p.ReliefProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            project.Status = ReliefProjectStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Project has been cancelled.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // IMAGE UPLOAD
        // =========================================================

        private async Task<string?> SaveImageAsync(
            IFormFile imageFile)
        {
            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var extension =
                Path.GetExtension(
                    imageFile.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            // 5 MB maximum
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                return null;
            }

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "projects");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder);
            }

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var filePath = Path.Combine(
                uploadsFolder,
                fileName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);

            await imageFile.CopyToAsync(stream);

            return $"/uploads/projects/{fileName}";
        }

        // =========================================================
        // DELETE IMAGE
        // =========================================================

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            var relativePath =
                imageUrl.TrimStart('/')
                         .Replace(
                             '/',
                             Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // =========================================================
        // PROJECT EXISTS
        // =========================================================

        private bool ProjectExists(int id)
        {
            return _context.ReliefProjects
                .Any(e => e.ReliefProjectId == id);
        }
    }
}