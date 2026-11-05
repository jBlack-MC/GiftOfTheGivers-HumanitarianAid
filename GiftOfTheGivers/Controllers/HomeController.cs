using System.Diagnostics;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDonationService _donationService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ICurrencyConverter _currencyConverter;
        private readonly IEmailService _emailService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly CachingOptions _caching;

        public HomeController(
            ApplicationDbContext context,
            IDonationService donationService,
            IPaymentGateway paymentGateway,
            ICurrencyConverter currencyConverter,
            IEmailService emailService,
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            IMemoryCache cache,
            IOptions<CachingOptions> caching)
        {
            _context = context;
            _donationService = donationService;
            _paymentGateway = paymentGateway;
            _currencyConverter = currencyConverter;
            _emailService = emailService;
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;
            _caching = caching.Value;
        }

        public async Task<IActionResult> Index()
        {
            Task<List<ReliefProject>> LoadFeaturedAsync() => _context.ReliefProjects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderByDescending(p => p.CreatedDate)
                .Take(3)
                .ToListAsync();

            // Cached briefly: the home page is the busiest page and was the stress-test bottleneck.
            var featuredProjects = _caching.PublicPageSeconds > 0
                ? await _cache.GetOrCreateAsync("featured-projects", entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_caching.PublicPageSeconds);
                    return LoadFeaturedAsync();
                })
                : await LoadFeaturedAsync();

            return View(featuredProjects);
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> ReliefProjects()
        {
            var projects = await _context.ReliefProjects
                .AsNoTracking()
                .OrderBy(p => p.Status)
                .ThenByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(projects);
        }

        [HttpGet]
        public async Task<IActionResult> Donate(int? projectId, string? cancelled)
        {
            var model = new DonateViewModel { ReliefProjectId = projectId };
            ViewData["PaymentCancelled"] = !string.IsNullOrEmpty(cancelled);

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is not null)
                {
                    model.DonorName = user.FullName;
                    model.DonorEmail = user.Email ?? string.Empty;
                }
            }

            await PopulateDonateModelAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Donate(DonateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDonateModelAsync(model);
                return View(model);
            }

            var result = await _donationService.CaptureAsync(model, User.UserId(), HttpContext.RequestAborted);
            if (result.Outcome == CaptureOutcome.ProjectUnavailable)
            {
                ModelState.AddModelError(nameof(model.ReliefProjectId),
                    "The selected project is not accepting donations. Please choose another project.");
                await PopulateDonateModelAsync(model);
                return View(model);
            }

            var donation = result.Donation!;
            if (_paymentGateway.IsEnabled && donation.PaymentStatus == PaymentStatus.Pending)
            {
                return Redirect(_paymentGateway.BuildCheckoutRedirectUrl(donation, BuildCheckoutUrls(donation)));
            }

            return RedirectToAction(nameof(Confirmation), new { reference = donation.TransactionReference });
        }

        private CheckoutUrls BuildCheckoutUrls(Donation donation)
        {
            string Absolute(string configured, string? fallback) =>
                !string.IsNullOrWhiteSpace(configured) ? configured : fallback!;

            var reference = donation.TransactionReference!;
            var returnUrl = Absolute(_configuration["PayFast:ReturnUrl"] ?? string.Empty,
                Url.Action(nameof(Confirmation), "Home", null, Request.Scheme));
            var cancelUrl = Absolute(_configuration["PayFast:CancelUrl"] ?? string.Empty,
                Url.Action(nameof(Donate), "Home", null, Request.Scheme));
            var notifyUrl = Absolute(_configuration["PayFast:NotifyUrl"] ?? string.Empty,
                Url.Action("Notify", "Payments", null, Request.Scheme));

            return new CheckoutUrls(
                QueryString(returnUrl, "reference", reference),
                QueryString(cancelUrl, "cancelled", reference),
                notifyUrl);
        }

        private static string QueryString(string url, string key, string value) =>
            Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, key, value);

        private async Task PopulateDonateModelAsync(DonateViewModel model)
        {
            model.AvailableProjects = await _context.ReliefProjects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderBy(p => p.Title)
                .ToListAsync();
            model.ProjectTitle = model.AvailableProjects.FirstOrDefault(p => p.Id == model.ReliefProjectId)?.Title;
            model.RatesToZar = _currencyConverter.RatesToZar;
            model.PaymentsEnabled = _paymentGateway.IsEnabled;
            model.PaymentsSandbox = _paymentGateway.IsSandbox;
        }

        /// <summary>
        /// Receipt page. Looked up by the unguessable transaction reference (not the sequential id),
        /// so it works both after a direct submit and when PayFast returns the donor.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("Donate/Confirmation")]
        public async Task<IActionResult> Confirmation(string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return RedirectToAction(nameof(Donate));
            }

            var donation = await _context.Donations
                .AsNoTracking()
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.TransactionReference == reference);
            if (donation is null)
            {
                return NotFound();
            }

            var userId = User.UserId();
            return View("DonationSuccess", new DonationReceiptViewModel
            {
                Donation = donation,
                PaymentsEnabled = _paymentGateway.IsEnabled,
                CanViewCertificate = donation.PaymentStatus == PaymentStatus.Verified &&
                    userId is not null &&
                    (donation.DonorId == userId || User.IsInRole(ClaimsPrincipalExtensions.EmployeeRole))
            });
        }

        // Legacy certificate URLs; the Donor controller is now the single implementation.
        [Authorize(Roles = "Donor,Employee")]
        [HttpGet]
        [Route("Donate/Tax-Certificate/{id:int}")]
        public IActionResult TaxCertificate(int id) =>
            RedirectToAction("TaxCertificate", "Donor", new { id });

        [Authorize(Roles = "Donor,Employee")]
        [HttpGet]
        [Route("Donate/Tax-Certificate/{id:int}/download")]
        public IActionResult DownloadTaxCertificate(int id) =>
            RedirectToAction("DownloadTaxCertificate", "Donor", new { id });

        [HttpGet]
        public IActionResult Volunteer()
        {
            return View(new VolunteerApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Volunteer(VolunteerApplicationViewModel model)
        {
            if (model.DateOfBirth.HasValue &&
                (model.DateOfBirth.Value > DateTime.UtcNow.AddYears(-16) || model.DateOfBirth.Value < DateTime.UtcNow.AddYears(-110)))
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Volunteers must be at least 16 years old.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameParts = model.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var volunteer = new Volunteer
            {
                UserId = User.UserId(),
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                Email = model.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                Address = model.Address,
                City = model.City,
                Province = model.Province,
                PostalCode = model.PostalCode,
                DateOfBirth = model.DateOfBirth?.Date,
                Skills = model.Skills,
                Availability = model.Availability,
                EmergencyContactName = model.EmergencyContactName,
                EmergencyContactPhone = model.EmergencyContactPhone,
                Status = VolunteerStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                PrivacyConsentAt = DateTime.UtcNow
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(VolunteerConfirmation));
        }

        [HttpGet]
        [Route("Volunteer/Confirmation")]
        [Route("Home/VolunteerConfirmation")]
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var inquiry = new Inquiry
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                Subject = model.Subject.Trim(),
                Message = model.Message,
                SubmittedDate = DateTime.UtcNow,
                PrivacyConsentAt = DateTime.UtcNow
            };

            _context.Inquiries.Add(inquiry);
            await _context.SaveChangesAsync();
            await _emailService.SendContactAcknowledgementAsync(inquiry);
            return RedirectToAction(nameof(ContactConfirmation));
        }

        [HttpGet]
        [Route("Contact/Confirmation")]
        [Route("Home/ContactConfirmation")]
        public IActionResult ContactConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Route("Apk")]
        [Route("Mobile")]
        [Route("MobileApp")]
        public IActionResult Apk()
        {
            var apkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", "GiftOfTheGivers.apk");
            var apkExists = System.IO.File.Exists(apkPath);
            ViewData["ApkExists"] = apkExists;
            ViewData["ApkSize"] = apkExists
                ? $"{new FileInfo(apkPath).Length / (1024.0 * 1024.0):F1} MB"
                : "12.4 MB";
            ViewData["AppVersion"] = "1.0.0";
            ViewData["MinAndroid"] = "7.0 (API 24+)";
            return View();
        }

        [HttpGet]
        [Route("downloads/giftofthegivers.apk")]
        [Route("Apk/Download")]
        public IActionResult DownloadApk()
        {
            var apkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", "GiftOfTheGivers.apk");
            if (System.IO.File.Exists(apkPath))
            {
                return PhysicalFile(apkPath, "application/vnd.android.package-archive", "GiftOfTheGivers.apk");
            }

            var content = System.Text.Encoding.UTF8.GetBytes("Gift of the Givers Humanitarian Aid Mobile App (Demo APK Package)");
            return File(content, "application/vnd.android.package-archive", "GiftOfTheGivers.apk");
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
