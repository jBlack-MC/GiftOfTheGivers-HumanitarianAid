using System.Globalization;
using System.Text;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Controllers
{
    [Authorize(Roles = ClaimsPrincipalExtensions.EmployeeRole)]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _audit;
        private readonly IDonationService _donationService;
        private readonly UserManager<AppUser> _userManager;

        public EmployeeController(
            ApplicationDbContext context,
            IEmailService emailService,
            IAuditService audit,
            IDonationService donationService,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _audit = audit;
            _donationService = donationService;
            _userManager = userManager;
        }

        private string CurrentUserId => User.UserId()!;

        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var verifiedThisMonth = _context.Donations
                .Where(d => d.PaymentStatus == PaymentStatus.Verified && d.DonationDate >= firstDayOfMonth);

            var model = new EmployeeDashboardViewModel
            {
                ActiveProjects = await _context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Active),
                PendingVolunteers = await _context.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Pending),
                TotalDonationsThisMonth = await verifiedThisMonth.CountAsync(),
                TotalAmountRaisedThisMonth = await verifiedThisMonth.SumAsync(d => (decimal?)d.AmountZar) ?? 0m,
                PendingPayments = await _context.Donations.CountAsync(d => d.PaymentStatus == PaymentStatus.Pending),
                OpenInquiries = await _context.Inquiries.CountAsync(i => !i.Handled),
                ProjectsOutOfBalance = await _context.ReliefProjects.CountAsync(p =>
                    p.FundsRaised != p.OfflineFundsZar + (p.Donations
                        .Where(d => d.PaymentStatus == PaymentStatus.Verified)
                        .Sum(d => (decimal?)d.AmountZar) ?? 0m)),
                RecentProjects = await _context.ReliefProjects.AsNoTracking()
                    .OrderByDescending(p => p.CreatedDate).Take(5).ToListAsync(),
                RecentVolunteerApplications = await _context.Volunteers.AsNoTracking()
                    .Where(v => v.Status == VolunteerStatus.Pending)
                    .OrderByDescending(v => v.ApplicationDate).Take(5).ToListAsync()
            };

            return View(model);
        }

        // ---------------------------------------------------------------- Relief projects

        public async Task<IActionResult> ReliefProjects()
        {
            var projects = await _context.ReliefProjects
                .AsNoTracking()
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
            ValidateProjectDates(input);
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var project = new ReliefProject
            {
                Title = input.Title,
                Description = input.Description,
                Location = input.Location,
                Status = input.Status,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                FundsRequired = input.FundsRequired,
                OfflineFundsZar = input.OfflineFundsZar,
                FundsRaised = input.OfflineFundsZar,
                ImageUrl = input.ImageUrl,
                RowVersion = Array.Empty<byte>(),
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = CurrentUserId
            };
            _context.ReliefProjects.Add(project);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "ProjectCreated", nameof(ReliefProject), project.Id,
                $"'{project.Title}', goal {Money.Zar(project.FundsRequired)}, offline funds {Money.Zar(project.OfflineFundsZar)}.");

            TempData["SuccessMessage"] = "Relief project created successfully.";
            return RedirectToAction(nameof(ReliefProjects));
        }

        [HttpGet]
        public async Task<IActionResult> EditReliefProject(int? id)
        {
            var project = id is null ? null : await _context.ReliefProjects.FindAsync(id);
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
                OfflineFundsZar = project.OfflineFundsZar,
                ImageUrl = project.ImageUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReliefProject(EditReliefProjectInput input)
        {
            ValidateProjectDates(input);
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var project = await _context.ReliefProjects.FindAsync(input.Id);
            if (project is null)
            {
                return NotFound();
            }

            var changes = new List<string>();
            if (project.Status != input.Status) changes.Add($"status {project.Status}→{input.Status}");
            if (project.FundsRequired != input.FundsRequired) changes.Add($"goal {Money.Zar(project.FundsRequired)}→{Money.Zar(input.FundsRequired)}");
            if (project.OfflineFundsZar != input.OfflineFundsZar) changes.Add($"offline funds {Money.Zar(project.OfflineFundsZar)}→{Money.Zar(input.OfflineFundsZar)}");
            if (project.Title != input.Title) changes.Add("title");
            if (project.Description != input.Description) changes.Add("description");

            // Offline funds feed FundsRaised; apply the difference so verified online donations
            // already counted are preserved.
            project.FundsRaised += input.OfflineFundsZar - project.OfflineFundsZar;
            project.OfflineFundsZar = input.OfflineFundsZar;
            project.Title = input.Title;
            project.Description = input.Description;
            project.Location = input.Location;
            project.Status = input.Status;
            project.StartDate = input.StartDate;
            project.EndDate = input.EndDate;
            project.FundsRequired = input.FundsRequired;
            project.ImageUrl = input.ImageUrl;
            _audit.Record(CurrentUserId, "ProjectEdited", nameof(ReliefProject), project.Id,
                changes.Count == 0 ? "No material changes." : string.Join("; ", changes));

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty,
                    "This project was changed by someone else (for example a payment was just verified). Reload the page and try again.");
                return View(input);
            }

            TempData["SuccessMessage"] = "Relief project updated successfully.";
            return RedirectToAction(nameof(ReliefProjects));
        }

        private void ValidateProjectDates(CreateReliefProjectInput input)
        {
            if (input.EndDate.HasValue && input.EndDate.Value < input.StartDate)
            {
                ModelState.AddModelError(nameof(input.EndDate), "End date cannot be before the start date.");
            }
        }

        // ---------------------------------------------------------------- Project updates

        [HttpGet]
        public async Task<IActionResult> CreateProjectUpdate(int? projectId)
        {
            var project = projectId is null ? null : await _context.ReliefProjects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectTitle = project.Title;
            return View(new CreateProjectUpdateInput { ReliefProjectId = project.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectUpdate(CreateProjectUpdateInput input)
        {
            var project = await _context.ReliefProjects.FindAsync(input.ReliefProjectId);
            if (project is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectTitle = project.Title;
                return View(input);
            }

            var update = new ProjectUpdate
            {
                ReliefProjectId = input.ReliefProjectId,
                Title = input.Title,
                Content = input.Content,
                ImageUrl = input.ImageUrl,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "Unknown",
                PostedByUserId = CurrentUserId
            };
            _context.ProjectUpdates.Add(update);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(CurrentUserId, "ProjectUpdatePosted", nameof(ProjectUpdate), update.Id,
                $"'{update.Title}' on project '{project.Title}'.");

            TempData["SuccessMessage"] = "Project update created successfully.";
            return RedirectToAction(nameof(ReliefProjects));
        }

        [HttpGet]
        public async Task<IActionResult> EditProjectUpdate(int? id)
        {
            var update = id is null
                ? null
                : await _context.ProjectUpdates.Include(u => u.ReliefProject).FirstOrDefaultAsync(u => u.Id == id);
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
            var update = await _context.ProjectUpdates.Include(u => u.ReliefProject).FirstOrDefaultAsync(u => u.Id == input.Id);
            if (update is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectTitle = update.ReliefProject?.Title ?? "Unknown";
                return View(input);
            }

            update.Title = input.Title;
            update.Content = input.Content;
            update.ImageUrl = input.ImageUrl;
            update.LastModifiedDate = DateTime.UtcNow;
            _audit.Record(CurrentUserId, "ProjectUpdateEdited", nameof(ProjectUpdate), update.Id, $"'{update.Title}'.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Project update modified successfully.";
            return RedirectToAction(nameof(ReliefProjects));
        }

        // ---------------------------------------------------------------- Volunteers

        public async Task<IActionResult> Volunteers(VolunteerStatus? status, int? page)
        {
            var query = _context.Volunteers.AsNoTracking();
            var filtered = status is null ? query : query.Where(v => v.Status == status);
            var pageNumber = PagedList<Volunteer>.NormalisePage(page);

            var model = new VolunteersPageViewModel
            {
                StatusFilter = status,
                Volunteers = await ToPageAsync(filtered.OrderByDescending(v => v.ApplicationDate), pageNumber),
                AllCount = await query.CountAsync(),
                PendingCount = await query.CountAsync(v => v.Status == VolunteerStatus.Pending),
                ApprovedCount = await query.CountAsync(v => v.Status == VolunteerStatus.Approved),
                RejectedCount = await query.CountAsync(v => v.Status == VolunteerStatus.Rejected)
            };
            return View(model);
        }

        public async Task<IActionResult> VolunteerDetails(int? id)
        {
            var volunteer = id is null ? null : await _context.Volunteers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (volunteer == null)
            {
                return NotFound();
            }

            var assignments = await _context.VolunteerAssignments.AsNoTracking()
                .Include(a => a.ReliefProject)
                .Where(a => a.VolunteerId == volunteer.Id)
                .OrderBy(a => a.ReliefProject!.Title)
                .ToListAsync();
            var assignedIds = assignments.Select(a => a.ReliefProjectId).ToList();

            return View(new VolunteerDetailsViewModel
            {
                Volunteer = volunteer,
                Assignments = assignments,
                AssignableProjects = await _context.ReliefProjects.AsNoTracking()
                    .Where(p => p.Status == ProjectStatus.Active && !assignedIds.Contains(p.Id))
                    .OrderBy(p => p.Title)
                    .ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ApproveVolunteer(int id) => SetVolunteerStatusAsync(id, VolunteerStatus.Approved);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RejectVolunteer(int id) => SetVolunteerStatusAsync(id, VolunteerStatus.Rejected);

        private async Task<IActionResult> SetVolunteerStatusAsync(int id, VolunteerStatus status)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound();
            }

            var previous = volunteer.Status;
            volunteer.Status = status;
            volunteer.ApprovalDate = status == VolunteerStatus.Approved ? DateTime.UtcNow : null;
            _audit.Record(CurrentUserId, status == VolunteerStatus.Approved ? "VolunteerApproved" : "VolunteerRejected",
                nameof(Volunteer), volunteer.Id, $"{previous} → {status}.");
            await _context.SaveChangesAsync();
            await _emailService.SendVolunteerStatusEmailAsync(volunteer);

            TempData[status == VolunteerStatus.Approved ? "SuccessMessage" : "InfoMessage"] =
                $"Volunteer application for {volunteer.FirstName} {volunteer.LastName} has been {status.ToString().ToLowerInvariant()}.";
            return RedirectToAction(nameof(VolunteerDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignVolunteer(int volunteerId, int reliefProjectId)
        {
            var volunteer = await _context.Volunteers.FindAsync(volunteerId);
            var project = await _context.ReliefProjects.FindAsync(reliefProjectId);
            if (volunteer is null || project is null)
            {
                return NotFound();
            }

            if (volunteer.Status != VolunteerStatus.Approved || project.Status != ProjectStatus.Active)
            {
                TempData["InfoMessage"] = "Only approved volunteers can be assigned, and only to active projects.";
            }
            else if (!await _context.VolunteerAssignments.AnyAsync(a => a.VolunteerId == volunteerId && a.ReliefProjectId == reliefProjectId))
            {
                var assignment = new VolunteerAssignment
                {
                    VolunteerId = volunteerId,
                    ReliefProjectId = reliefProjectId,
                    AssignedDate = DateTime.UtcNow
                };
                _context.VolunteerAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                await _audit.LogAsync(CurrentUserId, "VolunteerAssigned", nameof(VolunteerAssignment), assignment.Id,
                    $"{volunteer.FirstName} {volunteer.LastName} → '{project.Title}'.");
                TempData["SuccessMessage"] = $"Assigned to {project.Title}.";
            }

            return RedirectToAction(nameof(VolunteerDetails), new { id = volunteerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignVolunteer(int assignmentId)
        {
            var assignment = await _context.VolunteerAssignments
                .Include(a => a.ReliefProject)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
            if (assignment is null)
            {
                return NotFound();
            }

            _context.VolunteerAssignments.Remove(assignment);
            _audit.Record(CurrentUserId, "VolunteerUnassigned", nameof(VolunteerAssignment), assignment.Id,
                $"Volunteer {assignment.VolunteerId} removed from '{assignment.ReliefProject?.Title}'.");
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Assignment removed.";
            return RedirectToAction(nameof(VolunteerDetails), new { id = assignment.VolunteerId });
        }

        // ---------------------------------------------------------------- Donations

        public async Task<IActionResult> Donations(PaymentStatus? status, int? page)
        {
            var query = _context.Donations.AsNoTracking();
            var filtered = status is null ? query : query.Where(d => d.PaymentStatus == status);
            var verified = query.Where(d => d.PaymentStatus == PaymentStatus.Verified);

            var model = new DonationsPageViewModel
            {
                StatusFilter = status,
                Donations = await ToPageAsync(
                    filtered.Include(d => d.Donor).Include(d => d.ReliefProject).OrderByDescending(d => d.DonationDate),
                    PagedList<Donation>.NormalisePage(page)),
                VerifiedCount = await verified.CountAsync(),
                VerifiedTotalZar = await verified.SumAsync(d => (decimal?)d.AmountZar) ?? 0m,
                PendingCount = await query.CountAsync(d => d.PaymentStatus == PaymentStatus.Pending)
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDonations(PaymentStatus? status)
        {
            var query = _context.Donations.AsNoTracking().Include(d => d.ReliefProject).AsQueryable();
            if (status is not null)
            {
                query = query.Where(d => d.PaymentStatus == status);
            }

            var donations = await query.OrderBy(d => d.DonationDate).ToListAsync();
            var csv = Csv.Build(
                ["DonationDateUtc", "Reference", "Status", "Type", "Currency", "Amount", "ExchangeRateToZar", "AmountZar",
                 "PaymentMethod", "Project", "Anonymous", "DonorName", "DonorEmail", "VerifiedAtUtc", "ProviderPaymentId"],
                donations.Select(d => new[]
                {
                    d.DonationDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    d.TransactionReference,
                    d.PaymentStatus.ToString(),
                    d.DonationType,
                    d.Currency,
                    d.Amount.ToString("F2", CultureInfo.InvariantCulture),
                    d.ExchangeRateToZar.ToString("F6", CultureInfo.InvariantCulture),
                    d.AmountZar.ToString("F2", CultureInfo.InvariantCulture),
                    d.PaymentMethod,
                    d.ReliefProject?.Title ?? "General Fund",
                    d.IsAnonymous ? "Yes" : "No",
                    d.IsAnonymous ? string.Empty : d.DonorName,
                    d.IsAnonymous ? string.Empty : d.DonorEmail,
                    d.PaymentVerifiedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    d.ProviderPaymentId
                }));

            await _audit.LogAsync(CurrentUserId, "DonationsExported", nameof(Donation), 0,
                $"{donations.Count} rows, filter {status?.ToString() ?? "All"}.");

            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
                "text/csv", $"donations-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
        }

        // ---------------------------------------------------------------- Funding reconciliation

        public async Task<IActionResult> FundingReconciliation()
        {
            var rows = await _context.ReliefProjects.AsNoTracking()
                .OrderBy(p => p.Title)
                .Select(p => new FundingReconciliationRow
                {
                    Project = p,
                    VerifiedOnlineZar = p.Donations
                        .Where(d => d.PaymentStatus == PaymentStatus.Verified)
                        .Sum(d => (decimal?)d.AmountZar) ?? 0m,
                    PendingOnlineZar = p.Donations
                        .Where(d => d.PaymentStatus == PaymentStatus.Pending)
                        .Sum(d => (decimal?)d.AmountZar) ?? 0m
                })
                .ToListAsync();

            return View(rows);
        }

        /// <summary>Resets FundsRaised to offline funds + verified donations.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateFunding(int id)
        {
            if (!await _context.ReliefProjects.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            var (before, after) = await _donationService.RecalculateFundsRaisedAsync(id);
            await _audit.LogAsync(CurrentUserId, "FundingRecalculated", nameof(ReliefProject), id,
                $"FundsRaised {Money.Zar(before)} → {Money.Zar(after)} (offline + verified donations).");

            TempData["SuccessMessage"] = $"Funding recalculated: {Money.Zar(before)} → {Money.Zar(after)}.";
            return RedirectToAction(nameof(FundingReconciliation));
        }

        /// <summary>
        /// Keeps the current FundsRaised and records the unexplained difference as offline funds.
        /// Use when the existing figure is known to be correct (e.g. historical cash collections).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFundingAsOffline(int id)
        {
            var project = await _context.ReliefProjects.FindAsync(id);
            if (project is null)
            {
                return NotFound();
            }

            var verified = await _context.Donations
                .Where(d => d.ReliefProjectId == id && d.PaymentStatus == PaymentStatus.Verified)
                .SumAsync(d => (decimal?)d.AmountZar) ?? 0m;
            var newOffline = project.FundsRaised - verified;
            if (newOffline < 0m)
            {
                TempData["InfoMessage"] = "The current total is lower than the verified donations, so it cannot be accepted as offline funds. Recalculate instead.";
                return RedirectToAction(nameof(FundingReconciliation));
            }

            var previousOffline = project.OfflineFundsZar;
            project.OfflineFundsZar = newOffline;
            _audit.Record(CurrentUserId, "FundingAcceptedAsOffline", nameof(ReliefProject), id,
                $"Offline funds {Money.Zar(previousOffline)} → {Money.Zar(newOffline)}; FundsRaised kept at {Money.Zar(project.FundsRaised)}.");
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["InfoMessage"] = "The project changed while you were reviewing it. Please try again.";
                return RedirectToAction(nameof(FundingReconciliation));
            }

            TempData["SuccessMessage"] = $"'{project.Title}' accepted: offline funds now {Money.Zar(newOffline)}.";
            return RedirectToAction(nameof(FundingReconciliation));
        }

        // ---------------------------------------------------------------- Inquiries

        public async Task<IActionResult> Inquiries(int? page)
        {
            var inquiries = await ToPageAsync(
                _context.Inquiries.AsNoTracking().OrderBy(i => i.Handled).ThenByDescending(i => i.SubmittedDate),
                PagedList<Inquiry>.NormalisePage(page));
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
            _audit.Record(CurrentUserId, "InquiryHandled", nameof(Inquiry), inquiry.Id, $"'{inquiry.Subject}'.");
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Inquiries));
        }

        // ---------------------------------------------------------------- Audit log

        public async Task<IActionResult> AuditLog(int? page)
        {
            var query =
                from entry in _context.AuditLogs.AsNoTracking()
                join user in _context.Users.AsNoTracking() on entry.UserId equals user.Id into users
                from user in users.DefaultIfEmpty()
                orderby entry.Timestamp descending
                select new AuditLogEntryViewModel { Entry = entry, UserEmail = user != null ? user.Email : null };

            return View(await ToPageAsync(query, PagedList<AuditLogEntryViewModel>.NormalisePage(page), pageSize: 50));
        }

        // ---------------------------------------------------------------- Staff access

        public async Task<IActionResult> StaffAccess()
        {
            var employees = await _userManager.GetUsersInRoleAsync(ClaimsPrincipalExtensions.EmployeeRole);
            return View(new StaffAccessViewModel
            {
                Employees = employees.OrderBy(u => u.Email).ToList(),
                CurrentUserId = CurrentUserId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantEmployee(string email)
        {
            var user = string.IsNullOrWhiteSpace(email) ? null : await _userManager.FindByEmailAsync(email.Trim());
            if (user is null)
            {
                TempData["InfoMessage"] = "No account exists with that email. Ask the person to register first.";
            }
            else if (await _userManager.IsInRoleAsync(user, ClaimsPrincipalExtensions.EmployeeRole))
            {
                TempData["InfoMessage"] = $"{user.Email} already has staff access.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, ClaimsPrincipalExtensions.EmployeeRole);
                await _audit.LogAsync(CurrentUserId, "EmployeeGranted", nameof(AppUser), 0, $"Staff access granted to {user.Email}.");
                TempData["SuccessMessage"] = $"Staff access granted to {user.Email}.";
            }

            return RedirectToAction(nameof(StaffAccess));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeEmployee(string userId)
        {
            if (userId == CurrentUserId)
            {
                TempData["InfoMessage"] = "You cannot remove your own staff access.";
                return RedirectToAction(nameof(StaffAccess));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound();
            }

            await _userManager.RemoveFromRoleAsync(user, ClaimsPrincipalExtensions.EmployeeRole);
            // Rotating the security stamp makes existing sessions drop the role at their next
            // security-stamp validation (30 minutes by default) instead of when the cookie expires.
            await _userManager.UpdateSecurityStampAsync(user);
            await _audit.LogAsync(CurrentUserId, "EmployeeRevoked", nameof(AppUser), 0, $"Staff access removed from {user.Email}.");

            TempData["SuccessMessage"] = $"Staff access removed from {user.Email}.";
            return RedirectToAction(nameof(StaffAccess));
        }

        // ---------------------------------------------------------------- Helpers

        private static async Task<PagedList<T>> ToPageAsync<T>(IQueryable<T> query, int page, int pageSize = PagedList<T>.DefaultPageSize)
        {
            var total = await query.CountAsync();
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Min(page, lastPage);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }
    }
}
