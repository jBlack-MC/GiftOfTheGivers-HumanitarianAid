using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GiftOfTheGivers.Services;

/// <summary>
/// Sends ASP.NET Identity emails (confirmation, password reset) through SendGrid when an API key
/// is configured. Without a key it logs that the email was not sent instead of silently dropping it.
/// </summary>
public sealed class IdentityEmailSender(
    IOptions<EmailOptions> options,
    ILogger<IdentityEmailSender> logger,
    ISendGridClient? client = null) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (client is null)
        {
            logger.LogWarning("Identity email '{Subject}' to {Email} was not sent: SendGrid:ApiKey is not configured.",
                subject, email);
            return;
        }

        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(options.Value.FromAddress, options.Value.FromName),
            new EmailAddress(email), subject, plainTextContent: null, htmlContent: htmlMessage);
        var response = await client.SendEmailAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("SendGrid rejected identity email '{Subject}' with status {Status}", subject, response.StatusCode);
        }
    }
}
