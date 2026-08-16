using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var activeProjects = await _context.ReliefProjects
                .Where(p => p.Status == "Active")
                .CountAsync();

            var pendingVolunteers = await _context.Volunteers
                .Where(v => v.Status == "Pending")
                .CountAsync();

            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var totalDonationsThisMonth = await _context.Donations
                .Where(d => d.DonationDate >= firstDayOfMonth)
                .CountAsync();

            var totalAmountThisMonth = await _context.Donations
                .Where(d => d.DonationDate >= firstDayOfMonth)
                .SumAsync(d => (decimal?)d.Amount) ?? 0;

            var recentProjects = await _context.ReliefProjects
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .ToListAsync();

            var recentVolunteers = await _context.Volunteers
                .Where(v => v.Status == "Pending")
                .OrderByDescending(v => v.ApplicationDate)
                .Take(5)
                .ToListAsync();

            var model = new EmployeeDashboardViewModel
            {
                ActiveProjects = activeProjects,
                PendingVolunteers = pendingVolunteers,
                TotalDonationsThisMonth = totalDonationsThisMonth,
                TotalAmountRaisedThisMonth = totalAmountThisMonth,
                RecentProjects = recentProjects,
                RecentVolunteerApplications = recentVolunteers
            };

            return View(model);
        }

        // Relief Projects Management
        public async Task<IActionResult> ReliefProjects()
        {
            var projects = await _context.ReliefProjects
                .Include(p => p.ProjectUpdates)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(projects);
        }

        [HttpGet]
        public IActionResult CreateReliefProject()
        {
            return View(new ReliefProject());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReliefProject(ReliefProject project)
        {
            if (ModelState.IsValid)
            {
                project.CreatedDate = DateTime.Now;
                _context.ReliefProjects.Add(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Relief project created successfully.";
                return RedirectToAction(nameof(ReliefProjects));
            }
            return View(project);
        }

        [HttpGet]
        public async Task<IActionResult> EditReliefProject(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.ReliefProjects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReliefProject(int id, ReliefProject project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Relief project updated successfully.";
                    return RedirectToAction(nameof(ReliefProjects));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ReliefProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(project);
        }

        // Project Updates Management
        [HttpGet]
        public async Task<IActionResult> CreateProjectUpdate(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }

            var project = await _context.ReliefProjects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectTitle = project.Title;
            var model = new ProjectUpdate { ReliefProjectId = projectId.Value };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectUpdate(ProjectUpdate update)
        {
            if (ModelState.IsValid)
            {
                update.CreatedDate = DateTime.Now;
                update.CreatedBy = User.Identity?.Name ?? "Unknown";
                _context.ProjectUpdates.Add(update);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project update created successfully.";
                return RedirectToAction(nameof(ReliefProjects));
            }

            var project = await _context.ReliefProjects.FindAsync(update.ReliefProjectId);
            ViewBag.ProjectTitle = project?.Title ?? "Unknown";
            return View(update);
        }

        [HttpGet]
        public async Task<IActionResult> EditProjectUpdate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var update = await _context.ProjectUpdates
                .Include(u => u.ReliefProject)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (update == null)
            {
                return NotFound();
            }

            ViewBag.ProjectTitle = update.ReliefProject?.Title ?? "Unknown";
            return View(update);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProjectUpdate(int id, ProjectUpdate update)
        {
            if (id != update.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    update.LastModifiedDate = DateTime.Now;
                    _context.Update(update);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project update modified successfully.";
                    return RedirectToAction(nameof(ReliefProjects));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProjectUpdateExists(update.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            var project = await _context.ReliefProjects.FindAsync(update.ReliefProjectId);
            ViewBag.ProjectTitle = project?.Title ?? "Unknown";
            return View(update);
        }

        // Volunteers Management
        public async Task<IActionResult> Volunteers()
        {
            var volunteers = await _context.Volunteers
                .OrderByDescending(v => v.ApplicationDate)
                .ToListAsync();
            return View(volunteers);
        }

        public async Task<IActionResult> VolunteerDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound();
            }

            return View(volunteer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound();
            }

            volunteer.Status = "Approved";
            volunteer.ApprovalDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Volunteer {volunteer.FirstName} {volunteer.LastName} has been approved.";
            return RedirectToAction(nameof(Volunteers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVolunteer(int id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound();
            }

            volunteer.Status = "Inactive";
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = $"Volunteer application for {volunteer.FirstName} {volunteer.LastName} has been marked as inactive.";
            return RedirectToAction(nameof(Volunteers));
        }

        // Donations Management
        public async Task<IActionResult> Donations()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.ReliefProject)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();
            return View(donations);
        }

        // Helper methods
        private async Task<bool> ReliefProjectExists(int id)
        {
            return await _context.ReliefProjects.AnyAsync(e => e.Id == id);
        }

        private async Task<bool> ProjectUpdateExists(int id)
        {
            return await _context.ProjectUpdates.AnyAsync(e => e.Id == id);
        }
    }
}
