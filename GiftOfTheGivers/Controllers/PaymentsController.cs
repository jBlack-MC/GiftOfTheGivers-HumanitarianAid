using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Mvc;

namespace GiftOfTheGivers.Controllers;

/// <summary>
/// PayFast Instant Transaction Notification (ITN) webhook. PayFast posts here server-to-server
/// after every payment attempt; it is the only place a donation becomes Verified.
/// </summary>
[Route("Payments")]
public class PaymentsController(
    IPaymentGateway paymentGateway,
    IDonationService donationService,
    IEmailService emailService,
    ILogger<PaymentsController> logger) : Controller
{
    [IgnoreAntiforgeryToken]
    [HttpPost("Notify")]
    public async Task<IActionResult> Notify()
    {
        var form = await Request.ReadFormAsync();
        var reference = form["m_payment_id"].ToString();

        if (!await paymentGateway.VerifyNotificationAsync(form))
        {
            logger.LogWarning("PayFast notification failed signature/validate check for reference {Reference}", reference);
            return BadRequest();
        }

        var (outcome, donation) = await donationService.ApplyPaymentNotificationAsync(
            new PaymentNotification(
                reference,
                form["payment_status"].ToString(),
                form["amount_gross"].ToString(),
                form["merchant_id"].ToString(),
                form["pf_payment_id"].ToString()),
            paymentGateway.MerchantId,
            HttpContext.RequestAborted);

        logger.LogInformation("PayFast ITN for {Reference}: {Outcome}", reference, outcome);

        switch (outcome)
        {
            case NotificationOutcome.Verified:
                await emailService.SendDonationConfirmationAsync(donation!);
                return Ok();
            case NotificationOutcome.AlreadyVerified:
            case NotificationOutcome.MarkedFailed:
            case NotificationOutcome.Ignored:
                return Ok();
            case NotificationOutcome.UnknownReference:
                return NotFound();
            default:
                // Merchant or amount mismatch: refuse, leave the donation Pending and alert via logs.
                return BadRequest();
        }
    }
}
