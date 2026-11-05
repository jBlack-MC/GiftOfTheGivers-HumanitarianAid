using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Mvc;

namespace GiftOfTheGivers.Controllers.Api;

[ApiController]
[Route("api/v1/inquiries")]
[IgnoreAntiforgeryToken]
public class InquiriesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public InquiriesApiController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    /// <summary>
    /// Submits a general contact inquiry.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateInquiry([FromBody] InquiryApiRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.PrivacyConsent)
        {
            return BadRequest(new { message = "You must accept the privacy terms to submit an inquiry." });
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Name, Email, Subject and Message are required." });
        }

        var inquiry = new Inquiry
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message,
            SubmittedDate = DateTime.UtcNow,
            PrivacyConsentAt = DateTime.UtcNow
        };

        _context.Inquiries.Add(inquiry);
        await _context.SaveChangesAsync();
        await _emailService.SendContactAcknowledgementAsync(inquiry);

        return Ok(new
        {
            success = true,
            message = "Thank you for contacting Gift of the Givers. Your inquiry has been received.",
            inquiryId = inquiry.Id,
            submittedDate = inquiry.SubmittedDate
        });
    }
}
