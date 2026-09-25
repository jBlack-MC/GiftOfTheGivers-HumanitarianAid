using GiftOfTheGivers.Models;

namespace GiftOfTheGivers.Services;

public sealed class NoOpEmailService : IEmailService
{
    public Task SendContactAcknowledgementAsync(Inquiry inquiry) => Task.CompletedTask;

    public Task SendVolunteerStatusEmailAsync(Volunteer volunteer) => Task.CompletedTask;

    public Task SendDonationConfirmationAsync(Donation donation) => Task.CompletedTask;
}
