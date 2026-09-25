using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Globalization;

namespace GiftOfTheGivers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IEmailService _emailService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            IAuditService auditService,
            IPaymentGateway paymentGateway,
            IEmailService emailService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _auditService = auditService;
            _paymentGateway = paymentGateway;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProjects = await _context.ReliefProjects
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderByDescending(p => p.CreatedDate)
                .Take(3)
                .ToListAsync();

            return View(featuredProjects);
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> ReliefProjects()
        {
            var projects = await _context.ReliefProjects
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(projects);
        }

        [HttpGet]
        public async Task<IActionResult> Donate(int? projectId)
        {
            var model = new DonateViewModel();

            if (projectId.HasValue)
            {
                var project = await _context.ReliefProjects.FindAsync(projectId.Value);
                if (project != null)
                {
                    model.ReliefProjectId = project.Id;
                    model.ProjectTitle = project.Title;
                }
            }

            model.AvailableProjects = await _context.ReliefProjects
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Donate(DonateViewModel model, int? projectId)
        {
            if (!ModelState.IsValid)
            {
                await PopulateProjectsAsync(model);
                return View(model);
            }

            var existingDonation = await _context.Donations
                .FirstOrDefaultAsync(d => d.IdempotencyKey == model.IdempotencyToken);
            if (existingDonation is not null)
            {
                TempData["Amount"] = existingDonation.Amount.ToString(CultureInfo.InvariantCulture);
                TempData["Currency"] = existingDonation.Currency;
                TempData["DonationType"] = existingDonation.DonationType;
                TempData["TransactionRef"] = existingDonation.TransactionReference;
                TempData["DonationId"] = existingDonation.Id;

                return RedirectToAction(nameof(Confirmation));
            }

            var selectedProjectId = model.ReliefProjectId ?? projectId;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var donation = new Donation
                {
                    Amount = model.Amount,
                    DonationDate = DateTime.UtcNow,
                    PaymentMethod = model.PaymentMethod,
                    Notes = model.Notes,
                    IsAnonymous = model.IsAnonymous,
                    DonorEmail = model.DonorEmail,
                    DonationType = model.DonationType,
                    Currency = model.Currency,
                    ReliefProjectId = selectedProjectId,
                    TransactionReference = Guid.NewGuid().ToString("N"),
                    IdempotencyKey = model.IdempotencyToken,
                    PaymentStatus = PaymentStatus.Pending,
                    DonorId = User.Identity?.IsAuthenticated == true
                        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        : null
                };

                _context.Donations.Add(donation);
                await _context.SaveChangesAsync();

                if (selectedProjectId.HasValue)
                {
                    var project = await _context.ReliefProjects.FindAsync(selectedProjectId.Value);
                    if (project is null)
                    {
                        ModelState.AddModelError(nameof(model.ReliefProjectId), "The selected project no longer exists.");
                        await transaction.RollbackAsync();
                        await PopulateProjectsAsync(model);
                        return View(model);
                    }

                    project.FundsRaised += model.Amount;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                TempData["Amount"] = donation.Amount.ToString(CultureInfo.InvariantCulture);
                TempData["Currency"] = donation.Currency;
                TempData["DonationType"] = donation.DonationType;
                TempData["TransactionRef"] = donation.TransactionReference;
                TempData["DonationId"] = donation.Id;

                return RedirectToAction(nameof(Confirmation));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty,
                    "This project's funding total just changed — please try again.");
                await PopulateProjectsAsync(model);
                return View(model);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task PopulateProjectsAsync(DonateViewModel model)
        {
            model.AvailableProjects = await _context.ReliefProjects
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Donate/Confirmation")]
        public IActionResult Confirmation()
        {
            // Retrieve donation details from TempData
            ViewBag.DonorName = TempData["DonorName"];
            ViewBag.DonorEmail = TempData["DonorEmail"];
            ViewBag.Amount = TempData["Amount"];
            ViewBag.Currency = TempData["Currency"];
            ViewBag.DonationType = TempData["DonationType"];
            ViewBag.TransactionRef = TempData["TransactionRef"];
            ViewBag.DonationId = TempData["DonationId"];

            return View("DonationSuccess");
        }

        [Authorize(Roles = "Donor,Employee")]
        [HttpGet]
        [Route("Donate/Tax-Certificate/{id}")]
        public async Task<IActionResult> TaxCertificate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .Include(d => d.Donor)
                .FirstOrDefaultAsync(d => d.Id == id);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (donation == null || (donation.DonorId != userId && !User.IsInRole("Employee")))
            {
                return NotFound();
            }

            await _auditService.LogAsync(userId!, "CertificateViewed", nameof(Donation), donation.Id);

            // Mark tax certificate as issued (if not already)
            if (!donation.TaxCertificateIssued)
            {
                donation.TaxCertificateIssued = true;
                await _context.SaveChangesAsync();
            }

            // Use the same view as DonorController
            return View("~/Views/Donor/TaxCertificate.cshtml", donation);
        }

        [Authorize(Roles = "Donor,Employee")]
        [HttpGet]
        [Route("Donate/Tax-Certificate/{id}/download")]
        public async Task<IActionResult> DownloadTaxCertificate(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .Include(d => d.Donor)
                .FirstOrDefaultAsync(d => d.Id == id);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (donation == null || (donation.DonorId != userId && !User.IsInRole("Employee")))
            {
                return NotFound();
            }

            await _auditService.LogAsync(userId!, "CertificateDownloaded", nameof(Donation), donation.Id);

            var pdf = Services.TaxCertificatePdf.Generate(donation);
            return File(pdf, "application/pdf",
                $"TaxCertificate_{GiftOfTheGivers.Helpers.TaxCertificateNumber.Format(donation.TransactionReference, donation.Id)}.pdf");
        }

        [HttpGet]
        public IActionResult Volunteer()
        {
            return View(new VolunteerApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Volunteer(VolunteerApplicationViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Split FullName into FirstName and LastName
                var nameParts = model.FullName.Trim().Split(' ', 2);
                var firstName = nameParts[0];
                var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

                var volunteer = new Volunteer
                {
                    UserId = User.Identity?.IsAuthenticated == true
                        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        : null,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber ?? string.Empty,
                    Address = model.Address,
                    City = model.City,
                    Province = model.Province,
                    PostalCode = model.PostalCode,
                    DateOfBirth = model.DateOfBirth ?? DateTime.Now.AddYears(-18),
                    Skills = model.Skills,
                    Availability = model.Availability,
                    EmergencyContactName = model.EmergencyContactName,
                    EmergencyContactPhone = model.EmergencyContactPhone,
                    Status = VolunteerStatus.Pending,
                    ApplicationDate = DateTime.Now
                };

                _context.Volunteers.Add(volunteer);
                await _context.SaveChangesAsync();

                return RedirectToAction("VolunteerConfirmation");
            }

            return View(model);
        }

        [HttpGet]
        [Route("Volunteer/Confirmation")]
        public IActionResult VolunteerConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
                if (ModelState.IsValid)
            {
                    var inquiry = new Inquiry
                    {
                        Name = model.Name,
                        Email = model.Email,
                        Subject = model.Subject,
                        Message = model.Message,
                        SubmittedDate = DateTime.UtcNow
                    };

                    _context.Inquiries.Add(inquiry);
                    await _context.SaveChangesAsync();
                    await _emailService.SendContactAcknowledgementAsync(inquiry);
                    return RedirectToAction(nameof(ContactConfirmation));
            }

            return View(model);
        }

        [HttpGet]
        [Route("Contact/Confirmation")]
        public IActionResult ContactConfirmation()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
