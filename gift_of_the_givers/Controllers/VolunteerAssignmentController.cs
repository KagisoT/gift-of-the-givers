using gift_of_the_givers.Data;
using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gift_of_the_givers.Controllers
{
    [Authorize(Roles = "Employee")]
    public class VolunteerAssignmentController : Controller
    {
        private readonly GiftOfTheGiversDbContext _context;

        public VolunteerAssignmentController(
            GiftOfTheGiversDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: VolunteerAssignment
        // ============================================================
        public async Task<IActionResult> Index(
            int? volunteerId,
            int? projectId,
            AssignmentStatus? status,
            string? search)
        {
            var query = _context.VolunteerAssignments
                .Include(a => a.Volunteer)
                .Include(a => a.ReliefProject)
                .AsNoTracking()
                .AsQueryable();

            if (volunteerId.HasValue)
            {
                query = query.Where(
                    a => a.VolunteerId == volunteerId.Value);
            }

            if (projectId.HasValue)
            {
                query = query.Where(
                    a => a.ReliefProjectId == projectId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(
                    a => a.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(a =>
                    (a.Volunteer != null &&
                     (
                         a.Volunteer.FirstName.Contains(search) ||
                         a.Volunteer.LastName.Contains(search) ||
                         a.Volunteer.Email.Contains(search)
                     ))
                    ||
                    (a.ReliefProject != null &&
                     a.ReliefProject.Name.Contains(search))
                    ||
                    a.Role.Contains(search));
            }

            var assignments = await query
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();

            ViewBag.VolunteerId = volunteerId;
            ViewBag.ProjectId = projectId;
            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(assignments);
        }

        // ============================================================
        // GET: VolunteerAssignment/Details/5
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var assignment = await _context.VolunteerAssignments
                .Include(a => a.Volunteer)
                .Include(a => a.ReliefProject)
                    .ThenInclude(p => p!.ProjectUpdates)
                .FirstOrDefaultAsync(
                    a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            return View(assignment);
        }

        // ============================================================
        // GET: VolunteerAssignment/Create
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Create(
            int? volunteerId,
            int? projectId)
        {
            await LoadFormData();

            var assignment = new VolunteerAssignment();

            if (volunteerId.HasValue)
            {
                var volunteerExists = await _context.Volunteers
                    .AnyAsync(v =>
                        v.VolunteerId == volunteerId.Value);

                if (!volunteerExists)
                    return NotFound();

                assignment.VolunteerId = volunteerId.Value;
            }

            if (projectId.HasValue)
            {
                var projectExists = await _context.ReliefProjects
                    .AnyAsync(p =>
                        p.ReliefProjectId == projectId.Value);

                if (!projectExists)
                    return NotFound();

                assignment.ReliefProjectId = projectId.Value;
            }

            return View(assignment);
        }

        // ============================================================
        // POST: VolunteerAssignment/Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            VolunteerAssignment assignment)
        {
            ModelState.Remove(nameof(assignment.Volunteer));
            ModelState.Remove(nameof(assignment.ReliefProject));

            if (!ModelState.IsValid)
            {
                await LoadFormData();
                return View(assignment);
            }

            // --------------------------------------------------------
            // Check volunteer
            // --------------------------------------------------------
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v =>
                    v.VolunteerId == assignment.VolunteerId);

            if (volunteer == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerId),
                    "The selected volunteer does not exist.");

                await LoadFormData();
                return View(assignment);
            }

            // --------------------------------------------------------
            // Only approved/active volunteers can be assigned
            // --------------------------------------------------------
            if (volunteer.Status != VolunteerStatus.Approved &&
                volunteer.Status != VolunteerStatus.Active)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerId),
                    "Only approved or active volunteers can be assigned to projects.");

                await LoadFormData();
                return View(assignment);
            }

            // --------------------------------------------------------
            // Check project
            // --------------------------------------------------------
            var project = await _context.ReliefProjects
                .FirstOrDefaultAsync(p =>
                    p.ReliefProjectId == assignment.ReliefProjectId);

            if (project == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.ReliefProjectId),
                    "The selected relief project does not exist.");

                await LoadFormData();
                return View(assignment);
            }

            // --------------------------------------------------------
            // Prevent assignments to completed/cancelled projects
            // --------------------------------------------------------
            if (project.Status == ReliefProjectStatus.Completed ||
                project.Status == ReliefProjectStatus.Cancelled)
            {
                ModelState.AddModelError(
                    nameof(assignment.ReliefProjectId),
                    "Volunteers cannot be assigned to a completed or cancelled project.");

                await LoadFormData();
                return View(assignment);
            }

            // --------------------------------------------------------
            // Prevent duplicate volunteer/project assignment
            // --------------------------------------------------------
            var alreadyAssigned = await _context.VolunteerAssignments
                .AnyAsync(a =>
                    a.VolunteerId == assignment.VolunteerId &&
                    a.ReliefProjectId == assignment.ReliefProjectId);

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This volunteer is already assigned to the selected project.");

                await LoadFormData();
                return View(assignment);
            }

            assignment.AssignedDate = DateTime.UtcNow;
            assignment.Status = AssignmentStatus.Assigned;

            _context.VolunteerAssignments.Add(assignment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer has been assigned to the project successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = assignment.VolunteerAssignmentId });
        }

        // ============================================================
        // GET: VolunteerAssignment/Edit/5
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var assignment = await _context.VolunteerAssignments
                .FirstOrDefaultAsync(
                    a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            await LoadFormData();

            return View(assignment);
        }

        // ============================================================
        // POST: VolunteerAssignment/Edit/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            VolunteerAssignment assignment)
        {
            if (id != assignment.VolunteerAssignmentId)
                return NotFound();

            ModelState.Remove(nameof(assignment.Volunteer));
            ModelState.Remove(nameof(assignment.ReliefProject));

            if (!ModelState.IsValid)
            {
                await LoadFormData();
                return View(assignment);
            }

            var existingAssignment =
                await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(
                        a => a.VolunteerAssignmentId == id);

            if (existingAssignment == null)
                return NotFound();

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v =>
                    v.VolunteerId == assignment.VolunteerId);

            if (volunteer == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerId),
                    "The selected volunteer does not exist.");

                await LoadFormData();
                return View(assignment);
            }

            if (volunteer.Status != VolunteerStatus.Approved &&
                volunteer.Status != VolunteerStatus.Active)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerId),
                    "Only approved or active volunteers can be assigned.");

                await LoadFormData();
                return View(assignment);
            }

            var project = await _context.ReliefProjects
                .FirstOrDefaultAsync(p =>
                    p.ReliefProjectId == assignment.ReliefProjectId);

            if (project == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.ReliefProjectId),
                    "The selected project does not exist.");

                await LoadFormData();
                return View(assignment);
            }

            if (project.Status == ReliefProjectStatus.Completed ||
                project.Status == ReliefProjectStatus.Cancelled)
            {
                ModelState.AddModelError(
                    nameof(assignment.ReliefProjectId),
                    "Volunteers cannot be assigned to a completed or cancelled project.");

                await LoadFormData();
                return View(assignment);
            }

            // Check duplicate assignment, excluding current record.
            var duplicate = await _context.VolunteerAssignments
                .AnyAsync(a =>
                    a.VolunteerAssignmentId != id &&
                    a.VolunteerId == assignment.VolunteerId &&
                    a.ReliefProjectId == assignment.ReliefProjectId);

            if (duplicate)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This volunteer is already assigned to the selected project.");

                await LoadFormData();
                return View(assignment);
            }

            existingAssignment.VolunteerId =
                assignment.VolunteerId;

            existingAssignment.ReliefProjectId =
                assignment.ReliefProjectId;

            existingAssignment.Role =
                assignment.Role;

            // Status can be changed through dedicated actions.
            // This prevents an edit request from unexpectedly
            // changing assignment status.
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer assignment updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // GET: VolunteerAssignment/Delete/5
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var assignment = await _context.VolunteerAssignments
                .Include(a => a.Volunteer)
                .Include(a => a.ReliefProject)
                .FirstOrDefaultAsync(
                    a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            return View(assignment);
        }

        // ============================================================
        // POST: VolunteerAssignment/Delete/5
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment =
                await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(
                        a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            _context.VolunteerAssignments.Remove(assignment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer assignment removed successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // POST: VolunteerAssignment/Activate/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var assignment =
                await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(
                        a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            assignment.Status = AssignmentStatus.Active;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer assignment has been activated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // POST: VolunteerAssignment/Complete/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var assignment =
                await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(
                        a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            assignment.Status = AssignmentStatus.Completed;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer assignment has been completed.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // POST: VolunteerAssignment/Cancel/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var assignment =
                await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(
                        a => a.VolunteerAssignmentId == id);

            if (assignment == null)
                return NotFound();

            assignment.Status = AssignmentStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer assignment has been cancelled.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // ============================================================
        // Load volunteers and projects for forms
        // ============================================================
        private async Task LoadFormData()
        {
            ViewBag.Volunteers = await _context.Volunteers
                .Where(v =>
                    v.Status == VolunteerStatus.Approved ||
                    v.Status == VolunteerStatus.Active)
                .OrderBy(v => v.FirstName)
                .ThenBy(v => v.LastName)
                .ToListAsync();

            ViewBag.Projects = await _context.ReliefProjects
                .Where(p =>
                    p.Status != ReliefProjectStatus.Completed &&
                    p.Status != ReliefProjectStatus.Cancelled)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
    }
}