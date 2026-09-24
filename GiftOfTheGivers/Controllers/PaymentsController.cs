using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Controllers;

[Route("Payments")]
public class PaymentsController(
    ApplicationDbContext context,
    IPaymentGateway paymentGateway,
    IEmailService emailService,
    ILogger<PaymentsController> logger) : Controller
{
    [IgnoreAntiforgeryToken]
    [HttpPost("Notify")]
    public async Task<IActionResult> Notify()
    {
        if (!await paymentGateway.VerifyNotificationAsync(Request.Form))
        {
            logger.LogWarning("PayFast notification failed signature/validate check for reference {Reference}",
                Request.Form["m_payment_id"].ToString());
            return BadRequest();
        }

        var reference = Request.Form["m_payment_id"].ToString();
        var status = Request.Form["payment_status"].ToString();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var donation = await context.Donations
                .Include(d => d.ReliefProject)
                .FirstOrDefaultAsync(d => d.TransactionReference == reference);

            if (donation is null)
            {
                return NotFound();
            }

            if (donation.PaymentStatus == PaymentStatus.Verified)
            {
                return Ok();
            }

            if (status == "COMPLETE")
            {
                donation.PaymentStatus = PaymentStatus.Verified;
                if (donation.ReliefProject is not null)
                {
                    donation.ReliefProject.FundsRaised += donation.Amount;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                await emailService.SendDonationConfirmationAsync(donation);
            }
            else
            {
                donation.PaymentStatus = PaymentStatus.Failed;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return Ok();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}