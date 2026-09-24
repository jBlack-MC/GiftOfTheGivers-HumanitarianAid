using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Http;

namespace GiftOfTheGivers.Services;

public interface IPaymentGateway
{
    string BuildCheckoutRedirectUrl(Donation donation);
    Task<bool> VerifyNotificationAsync(IFormCollection payload);
}