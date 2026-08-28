using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GiftOfTheGivers.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorController(ApplicationDbContext context)
        {
            _context = context;
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
                .Where(p => p.Status == "Active")
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

            return View(donation);
        }

        public async Task<IActionResult> TaxCertificate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .Include(d => d.Donor)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == userId);

            if (donation == null)
            {
                return NotFound();
            }

            // Mark tax certificate as issued
            if (!donation.TaxCertificateIssued)
            {
                donation.TaxCertificateIssued = true;
                await _context.SaveChangesAsync();
            }

            return View(donation);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTaxCertificate(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donation = await _context.Donations
                .Include(d => d.ReliefProject)
                .Include(d => d.Donor)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == userId);

            if (donation == null)
            {
                return NotFound();
            }

            var pdf = Services.TaxCertificatePdf.Generate(donation);
            return File(pdf, "application/pdf",
                $"TaxCertificate_{donation.TransactionReference ?? donation.Id.ToString()}.pdf");
        }
    }
}
