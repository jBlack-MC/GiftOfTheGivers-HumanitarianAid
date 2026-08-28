using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GiftOfTheGivers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProjects = await _context.ReliefProjects
                .Where(p => p.Status == "Active")
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
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Donate(DonateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // PROTOTYPE/PART 1: This is a dummy donation record - no real payment processing
                // In production, this would integrate with a payment gateway

                var donation = new Donation
                {
                    DonorId = User.Identity?.IsAuthenticated == true
                        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        : null,
                    ReliefProjectId = model.ReliefProjectId,
                    DonationType = model.DonationType,
                    Currency = model.Currency,
                    Amount = model.Amount,
                    PaymentMethod = model.PaymentMethod,
                    IsAnonymous = model.IsAnonymous,
                    Notes = model.Notes,
                    DonationDate = DateTime.Now,
                    TransactionReference = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper()
                };

                _context.Donations.Add(donation);
                await _context.SaveChangesAsync();

                // Store donation details in TempData for confirmation page
                TempData["DonationId"] = donation.Id;
                TempData["DonorName"] = model.DonorName;
                TempData["DonorEmail"] = model.DonorEmail;
                TempData["Amount"] = model.Amount.ToString("N2");
                TempData["Currency"] = model.Currency;
                TempData["DonationType"] = model.DonationType;
                TempData["TransactionRef"] = donation.TransactionReference;

                return RedirectToAction("Confirmation");
            }

            model.AvailableProjects = await _context.ReliefProjects
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(model);
        }

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

            if (donation == null)
            {
                return NotFound();
            }

            // Mark tax certificate as issued (if not already)
            if (!donation.TaxCertificateIssued)
            {
                donation.TaxCertificateIssued = true;
                await _context.SaveChangesAsync();
            }

            // Use the same view as DonorController
            return View("~/Views/Donor/TaxCertificate.cshtml", donation);
        }

        [HttpGet]
        [Route("Donate/Tax-Certificate/{id}/download")]
        public async Task<IActionResult> DownloadTaxCertificate(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .Include(d => d.Donor)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation == null)
            {
                return NotFound();
            }

            var pdf = Services.TaxCertificatePdf.Generate(donation);
            return File(pdf, "application/pdf",
                $"TaxCertificate_{donation.TransactionReference ?? donation.Id.ToString()}.pdf");
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
                    Status = "Pending",
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
        public IActionResult Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // In a real application, this would send an email
                // For now, we'll just redirect to confirmation
                return RedirectToAction("ContactConfirmation");
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
