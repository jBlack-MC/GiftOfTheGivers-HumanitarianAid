using GiftOfTheGivers.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GiftOfTheGivers.Services;

public sealed class SendGridEmailService(ISendGridClient client, IOptions<EmailOptions> options) : IEmailService
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
            $"Your donation of {donation.Currency} {donation.Amount:N2} has been confirmed. Reference: {donation.TransactionReference}.");

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(_options.FromAddress, _options.FromName),
            new EmailAddress(toEmail), subject, body, body);
        await _client.SendEmailAsync(message);
    }
}

public sealed class EmailOptions
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Gift of the Givers";
}