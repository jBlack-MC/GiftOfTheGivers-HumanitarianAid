using GiftOfTheGivers.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GiftOfTheGivers.Services;

public sealed class SendGridEmailService(
    ISendGridClient client,
    IOptions<EmailOptions> options,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly ISendGridClient _client = client;
    private readonly EmailOptions _options = options.Value;

    public Task SendContactAcknowledgementAsync(Inquiry inquiry) =>
        SendAsync(inquiry.Email, "We received your message",
            $"Hi {inquiry.Name}, thanks for contacting Gift of the Givers. We will respond soon.");

    public Task SendVolunteerStatusEmailAsync(Volunteer volunteer) =>
        SendAsync(volunteer.Email, $"Your volunteer application: {volunteer.Status}",
            $"Hi {volunteer.FirstName}, your application status is now: {volunteer.Status}.");

    public Task SendDonationConfirmationAsync(Donation donation) =>
        SendAsync(donation.DonorEmail ?? string.Empty, "Thank you for your donation",
            $"Your donation of {donation.Currency} {donation.Amount:N2} (R {donation.AmountZar:N2}) has been confirmed. " +
            $"Reference: {donation.TransactionReference}. Your tax certificate is available in the donor portal.");

    /// <summary>
    /// Email is a courtesy that follows an already-committed change, so a delivery failure is
    /// logged instead of failing the request (which would invite the user to resubmit).
    /// </summary>
    private async Task SendAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        try
        {
            var message = MailHelper.CreateSingleEmail(
                new EmailAddress(_options.FromAddress, _options.FromName),
                new EmailAddress(toEmail), subject, body, body);
            var response = await _client.SendEmailAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("SendGrid rejected email '{Subject}' with status {Status}", subject, response.StatusCode);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send email '{Subject}'", subject);
        }
    }
}

public sealed class EmailOptions
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Gift of the Givers";
}
