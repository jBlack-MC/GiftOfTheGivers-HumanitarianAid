using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGivers.Services;

namespace GiftOfTheGivers.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public EmployeeController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var activeProjects = await _context.ReliefProjects
                .Where(p => p.Status == ProjectStatus.Active)
                .CountAsync();

            var pendingVolunteers = await _context.Volunteers
                .Where(v => v.Status == VolunteerStatus.Pending)
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
                .Where(v => v.Status == VolunteerStatus.Pending)
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
            return View(new CreateReliefProjectInput());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReliefProject(CreateReliefProjectInput input)
        {
            if (ModelState.IsValid)
            {
                var project = new ReliefProject
                {
                    Title = input.Title,
                    Description = input.Description,
                    Location = input.Location,
                    Status = input.Status,
                    StartDate = input.StartDate,
                    EndDate = input.EndDate,
                    FundsRequired = input.FundsRequired,
                    ImageUrl = input.ImageUrl,
                    FundsRaised = 0m,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                };
                _context.ReliefProjects.Add(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Relief project created successfully.";
                return RedirectToAction(nameof(ReliefProjects));
            }
            return View(input);
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

            return View(new EditReliefProjectInput
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Location = project.Location,
                Status = project.Status,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                FundsRequired = project.FundsRequired,
                ImageUrl = project.ImageUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReliefProject(EditReliefProjectInput input)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var project = await _context.ReliefProjects.FindAsync(input.Id);
                    if (project is null)
                    {
                        return NotFound();
                    }

                    project.Title = input.Title;
                    project.Description = input.Description;
                    project.Location = input.Location;
                    project.Status = input.Status;
                    project.StartDate = input.StartDate;
                    project.EndDate = input.EndDate;
                    project.FundsRequired = input.FundsRequired;
                    project.ImageUrl = input.ImageUrl;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Relief project updated successfully.";
                    return RedirectToAction(nameof(ReliefProjects));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ReliefProjectExists(input.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(input);
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
            var model = new CreateProjectUpdateInput { ReliefProjectId = projectId.Value };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectUpdate(CreateProjectUpdateInput input)
        {
            if (ModelState.IsValid)
            {
                var update = new ProjectUpdate
                {
                    ReliefProjectId = input.ReliefProjectId,
                    Title = input.Title,
                    Content = input.Content,
                    ImageUrl = input.ImageUrl,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "Unknown",
                    PostedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                };
                _context.ProjectUpdates.Add(update);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project update created successfully.";
                return RedirectToAction(nameof(ReliefProjects));
            }

            var project = await _context.ReliefProjects.FindAsync(input.ReliefProjectId);
            ViewBag.ProjectTitle = project?.Title ?? "Unknown";
            return View(input);
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
            return View(new EditProjectUpdateInput
            {
                Id = update.Id,
                ReliefProjectId = update.ReliefProjectId,
                Title = update.Title,
                Content = update.Content,
                ImageUrl = update.ImageUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProjectUpdate(EditProjectUpdateInput input)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var update = await _context.ProjectUpdates.FindAsync(input.Id);
                    if (update is null)
                    {
                        return NotFound();
                    }

                    update.Title = input.Title;
                    update.Content = input.Content;
                    update.ImageUrl = input.ImageUrl;
                    update.LastModifiedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project update modified successfully.";
                    return RedirectToAction(nameof(ReliefProjects));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProjectUpdateExists(input.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            var project = await _context.ReliefProjects.FindAsync(input.ReliefProjectId);
            ViewBag.ProjectTitle = project?.Title ?? "Unknown";
            return View(input);
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

            volunteer.Status = VolunteerStatus.Approved;
            volunteer.ApprovalDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _emailService.SendVolunteerStatusEmailAsync(volunteer);

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

            volunteer.Status = VolunteerStatus.Rejected;
            await _context.SaveChangesAsync();
            await _emailService.SendVolunteerStatusEmailAsync(volunteer);

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

        public async Task<IActionResult> Inquiries()
        {
            var inquiries = await _context.Inquiries
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
            return View(inquiries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInquiryHandled(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry is null)
            {
                return NotFound();
            }

            inquiry.Handled = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Inquiries));
        }

        public async Task<IActionResult> AuditLog()
        {
            var entries = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(200)
                .ToListAsync();
            return View(entries);
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
