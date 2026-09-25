using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GiftOfTheGivers.Services;

namespace GiftOfTheGivers.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public DonorController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalDonations = await _context.Donations
                .Where(d => d.DonorId == userId)
                .CountAsync();

            var totalAmount = await _context.Donations
                .Where(d => d.DonorId == userId)
                .SumAsync(d => (decimal?)d.Amount) ?? 0;

            var recentDonations = await _context.Donations
                .Where(d => d.DonorId == userId)
                .Include(d => d.ReliefProject)
                .OrderByDescending(d => d.DonationDate)
                .Take(5)
                .ToListAsync();

            var featuredProjects = await _context.ReliefProjects
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderByDescending(p => p.CreatedDate)
                .Take(3)
                .ToListAsync();

            var model = new DonorDashboardViewModel
            {
                TotalDonations = totalDonations,
                TotalAmountDonated = totalAmount,
                RecentDonations = recentDonations,
                FeaturedProjects = featuredProjects
            };

            return View(model);
        }

        public async Task<IActionResult> MyDonations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donations = await _context.Donations
                .Where(d => d.DonorId == userId)
                .Include(d => d.ReliefProject)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        public async Task<IActionResult> DonationDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == userId);

            if (donation == null)
            {
                return NotFound();
            }

            await _auditService.LogAsync(userId!, "CertificateViewed", nameof(Donation), donation.Id);

            return View(donation);
        }

        [Authorize(Roles = "Donor,Employee")]
        public async Task<IActionResult> TaxCertificate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.Id == id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isEmployee = User.IsInRole("Employee");

            if (donation is null || (donation.DonorId != userId && !isEmployee))
            {
                return NotFound();
            }

            await _auditService.LogAsync(userId!, "CertificateViewed", nameof(Donation), donation.Id);

            return View(donation);
        }

        [Authorize(Roles = "Donor,Employee")]
        [HttpGet]
        public async Task<IActionResult> DownloadTaxCertificate(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.Id == id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isEmployee = User.IsInRole("Employee");

            if (donation is null || (donation.DonorId != userId && !isEmployee))
            {
                return NotFound();
            }

            await _auditService.LogAsync(userId!, "CertificateDownloaded", nameof(Donation), donation.Id);

            var pdf = Services.TaxCertificatePdf.Generate(donation);
            return File(pdf, "application/pdf",
                $"TaxCertificate-{GiftOfTheGivers.Helpers.TaxCertificateNumber.Format(donation.TransactionReference, donation.Id)}.pdf");
        }
    }
}
