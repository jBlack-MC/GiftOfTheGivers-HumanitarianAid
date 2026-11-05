using GiftOfTheGivers.Data;
using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Controllers.Api;

[ApiController]
[Route("api/v1/donations")]
[IgnoreAntiforgeryToken]
public class DonationsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDonationService _donationService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICurrencyConverter _currencyConverter;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public DonationsApiController(
        ApplicationDbContext context,
        IDonationService donationService,
        IPaymentGateway paymentGateway,
        ICurrencyConverter currencyConverter,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _donationService = donationService;
        _paymentGateway = paymentGateway;
        _currencyConverter = currencyConverter;
        _userManager = userManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Captures a new donation from mobile or web API clients and returns checkout redirection URL.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DonationApiResponse>> CreateDonation([FromBody] CreateDonationApiRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Donation amount must be greater than zero." });
        }

        var model = new DonateViewModel
        {
            ReliefProjectId = request.ReliefProjectId,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ZAR" : request.Currency,
            DonationType = string.IsNullOrWhiteSpace(request.DonationType) ? "OneTime" : request.DonationType,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Card" : request.PaymentMethod,
            DonorName = request.DonorName,
            DonorEmail = request.DonorEmail,
            IsAnonymous = request.IsAnonymous,
            Notes = request.Notes,
            PrivacyConsent = true
        };

        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = User.UserId();
        }

        var result = await _donationService.CaptureAsync(model, userId, HttpContext.RequestAborted);
        if (result.Outcome == CaptureOutcome.ProjectUnavailable)
        {
            return BadRequest(new { message = "The selected relief project is unavailable or not accepting donations." });
        }

        var donation = result.Donation!;
        string? checkoutUrl = null;

        if (_paymentGateway.IsEnabled && donation.PaymentStatus == PaymentStatus.Pending)
        {
            var urls = BuildCheckoutUrls(donation);
            checkoutUrl = _paymentGateway.BuildCheckoutRedirectUrl(donation, urls);
        }

        var response = new DonationApiResponse
        {
            TransactionReference = donation.TransactionReference ?? string.Empty,
            Amount = donation.Amount,
            Currency = donation.Currency,
            AmountZar = donation.AmountZar,
            PaymentStatus = donation.PaymentStatus.ToString(),
            ReliefProjectId = donation.ReliefProjectId,
            ReliefProjectTitle = donation.ReliefProject?.Title,
            PaymentCheckoutUrl = checkoutUrl,
            DonationDate = donation.DonationDate
        };

        return CreatedAtAction(nameof(GetDonationByReference), new { reference = donation.TransactionReference }, response);
    }

    /// <summary>
    /// Retrieves live payment status and receipt details for a donation by reference.
    /// </summary>
    [HttpGet("{reference}")]
    public async Task<ActionResult<DonationApiResponse>> GetDonationByReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return BadRequest(new { message = "Transaction reference is required." });
        }

        var donation = await _context.Donations
            .AsNoTracking()
            .Include(d => d.ReliefProject)
            .FirstOrDefaultAsync(d => d.TransactionReference == reference);

        if (donation is null)
        {
            return NotFound(new { message = $"Donation with reference '{reference}' was not found." });
        }

        var response = new DonationApiResponse
        {
            TransactionReference = donation.TransactionReference ?? string.Empty,
            Amount = donation.Amount,
            Currency = donation.Currency,
            AmountZar = donation.AmountZar,
            PaymentStatus = donation.PaymentStatus.ToString(),
            ReliefProjectId = donation.ReliefProjectId,
            ReliefProjectTitle = donation.ReliefProject?.Title,
            DonationDate = donation.DonationDate
        };

        return Ok(response);
    }

    /// <summary>
    /// Returns donation history for the currently authenticated donor.
    /// </summary>
    [HttpGet("my-donations")]
    [Authorize]
    public async Task<ActionResult<List<DonationApiResponse>>> GetMyDonations()
    {
        var userId = User.UserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var list = await _context.Donations
            .AsNoTracking()
            .Include(d => d.ReliefProject)
            .Where(d => d.DonorId == userId)
            .OrderByDescending(d => d.DonationDate)
            .Select(d => new DonationApiResponse
            {
                TransactionReference = d.TransactionReference ?? string.Empty,
                Amount = d.Amount,
                Currency = d.Currency,
                AmountZar = d.AmountZar,
                PaymentStatus = d.PaymentStatus.ToString(),
                ReliefProjectId = d.ReliefProjectId,
                ReliefProjectTitle = d.ReliefProject != null ? d.ReliefProject.Title : null,
                DonationDate = d.DonationDate
            })
            .ToListAsync();

        return Ok(list);
    }

    private CheckoutUrls BuildCheckoutUrls(Donation donation)
    {
        string Absolute(string configured, string? fallback) =>
            !string.IsNullOrWhiteSpace(configured) ? configured : fallback!;

        var reference = donation.TransactionReference ?? string.Empty;
        var returnUrl = Absolute(_configuration["PayFast:ReturnUrl"] ?? string.Empty,
            Url.Action("Confirmation", "Home", new { reference }, Request.Scheme));
        var cancelUrl = Absolute(_configuration["PayFast:CancelUrl"] ?? string.Empty,
            Url.Action("Donate", "Home", null, Request.Scheme));
        var notifyUrl = Absolute(_configuration["PayFast:NotifyUrl"] ?? string.Empty,
            Url.Action("Notify", "Payments", null, Request.Scheme));

        return new CheckoutUrls(
            Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(returnUrl, "reference", reference),
            Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(cancelUrl, "cancelled", reference),
            notifyUrl);
    }
}
