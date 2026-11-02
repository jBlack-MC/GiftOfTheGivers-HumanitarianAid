using GiftOfTheGivers.Data;
using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Controllers
{
    // Roles are declared per action: ASP.NET Core requires every [Authorize] on the class AND the
    // action to pass, so a class-level Donor-only attribute would block employees from the
    // certificate actions below.
    [Authorize]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly OrganisationOptions _organisation;

        public DonorController(ApplicationDbContext context, IAuditService auditService, IOptions<OrganisationOptions> organisation)
        {
            _context = context;
            _auditService = auditService;
            _organisation = organisation.Value;
        }

        [Authorize(Roles = ClaimsPrincipalExtensions.DonorRole)]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.UserId();
            var mine = _context.Donations.AsNoTracking().Where(d => d.DonorId == userId);

            var model = new DonorDashboardViewModel
            {
                TotalDonations = await mine.CountAsync(d => d.PaymentStatus == PaymentStatus.Verified),
                TotalAmountDonated = await mine
                    .Where(d => d.PaymentStatus == PaymentStatus.Verified)
                    .SumAsync(d => (decimal?)d.AmountZar) ?? 0m,
                PendingDonations = await mine.CountAsync(d => d.PaymentStatus == PaymentStatus.Pending),
                RecentDonations = await mine
                    .Include(d => d.ReliefProject)
                    .OrderByDescending(d => d.DonationDate)
                    .Take(5)
                    .ToListAsync(),
                FeaturedProjects = await _context.ReliefProjects
                    .AsNoTracking()
                    .Where(p => p.Status == ProjectStatus.Active)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(3)
                    .ToListAsync()
            };

            return View(model);
        }

        [Authorize(Roles = ClaimsPrincipalExtensions.DonorRole)]
        public async Task<IActionResult> MyDonations()
        {
            var userId = User.UserId();

            var donations = await _context.Donations
                .AsNoTracking()
                .Where(d => d.DonorId == userId)
                .Include(d => d.ReliefProject)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        [Authorize(Roles = ClaimsPrincipalExtensions.DonorRole)]
        public async Task<IActionResult> DonationDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.UserId();
            var donation = await _context.Donations
                .AsNoTracking()
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == userId);

            return donation is null ? NotFound() : View(donation);
        }

        [Authorize(Roles = "Donor,Employee")]
        public async Task<IActionResult> TaxCertificate(int? id)
        {
            var donation = await FindCertificateDonationAsync(id);
            if (donation is null)
            {
                return NotFound();
            }

            await _auditService.LogAsync(User.UserId()!, "CertificateViewed", nameof(Donation), donation.Id);
            return View(donation);
        }

        [Authorize(Roles = "Donor,Employee")]
        [HttpGet]
        public async Task<IActionResult> DownloadTaxCertificate(int id)
        {
            var donation = await FindCertificateDonationAsync(id);
            if (donation is null)
            {
                return NotFound();
            }

            await _auditService.LogAsync(User.UserId()!, "CertificateDownloaded", nameof(Donation), donation.Id);

            var pdf = TaxCertificatePdf.Generate(donation, CertificateIssuer.From(_organisation));
            return File(pdf, "application/pdf",
                $"TaxCertificate-{GiftOfTheGivers.Helpers.TaxCertificateNumber.Format(donation.TransactionReference, donation.Id)}.pdf");
        }

        /// <summary>
        /// A certificate exists only for a verified (paid) donation that belongs to the current
        /// donor; employees may see any verified donation's certificate. Anything else is a 404
        /// so that ids of other people's donations are not confirmed.
        /// </summary>
        private async Task<Donation?> FindCertificateDonationAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            var donation = await _context.Donations
                .AsNoTracking()
                .Include(d => d.ReliefProject)
                .Include(d => d.Donor)
                .FirstOrDefaultAsync(d => d.Id == id && d.PaymentStatus == PaymentStatus.Verified);

            if (donation is null)
            {
                return null;
            }

            var isOwner = donation.DonorId is not null && donation.DonorId == User.UserId();
            return isOwner || User.IsInRole(ClaimsPrincipalExtensions.EmployeeRole) ? donation : null;
        }
    }
}
