using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Http;

namespace GiftOfTheGivers.Services;

/// <summary>Absolute URLs PayFast needs for one checkout.</summary>
public sealed record CheckoutUrls(string ReturnUrl, string CancelUrl, string NotifyUrl);

public interface IPaymentGateway
{
    /// <summary>False when payments are switched off; donations then stay Pending.</summary>
    bool IsEnabled { get; }

    bool IsSandbox { get; }

    /// <summary>Merchant id that ITN payloads must carry.</summary>
    string MerchantId { get; }

    /// <summary>Builds the signed PayFast URL for the donation's rand amount.</summary>
    string BuildCheckoutRedirectUrl(Donation donation, CheckoutUrls urls);

    /// <summary>Checks the ITN signature and confirms it with PayFast's validate endpoint.</summary>
    Task<bool> VerifyNotificationAsync(IFormCollection payload);
}
