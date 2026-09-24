using GiftOfTheGivers.Models;

namespace GiftOfTheGivers.Services;

public interface IEmailService
{
    Task SendContactAcknowledgementAsync(Inquiry inquiry);
    Task SendVolunteerStatusEmailAsync(Volunteer volunteer);
    Task SendDonationConfirmationAsync(Donation donation);
}