using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Services;

public sealed class PayFastPaymentService(IOptions<PayFastOptions> options, HttpClient httpClient) : IPaymentGateway
{
    private readonly PayFastOptions _options = options.Value;
    private readonly HttpClient _httpClient = httpClient;

    public string BuildCheckoutRedirectUrl(Donation donation)
    {
        var baseUrl = _options.Sandbox
            ? "https://sandbox.payfast.co.za/eng/process"
            : "https://www.payfast.co.za/eng/process";

        var fields = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", _options.MerchantId),
            new("merchant_key", _options.MerchantKey),
            new("return_url", _options.ReturnUrl),
            new("cancel_url", _options.CancelUrl),
            new("notify_url", _options.NotifyUrl),
            new("m_payment_id", donation.TransactionReference ?? string.Empty),
            new("amount", donation.Amount.ToString("F2", CultureInfo.InvariantCulture)),
            new("item_name", $"Donation - {donation.ReliefProject?.Title ?? "General Fund"}")
        };

        var signature = CreateSignature(fields);
        var query = string.Join("&", fields.Select(field => $"{field.Key}={UrlEncode(field.Value)}"));
        return $"{baseUrl}?{query}&signature={signature}";
    }

    public async Task<bool> VerifyNotificationAsync(IFormCollection payload)
    {
        // ITN signatures use the posted field order; alphabetically sorting fields changes the signed string.
        var fields = payload
            .Where(field => field.Key != "signature")
            .Select(field => new KeyValuePair<string, string>(field.Key, field.Value.ToString()))
            .ToList();

        if (!payload.TryGetValue("signature", out var providedSignature) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(CreateSignature(fields)),
                Encoding.ASCII.GetBytes(providedSignature.ToString())))
        {
            return false;
        }

        var validateUrl = _options.Sandbox
            ? "https://sandbox.payfast.co.za/eng/query/validate"
            : "https://www.payfast.co.za/eng/query/validate";
        using var response = await _httpClient.PostAsync(validateUrl, new FormUrlEncodedContent(fields));
        var body = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode && body.Trim() == "VALID";
    }

    private string CreateSignature(IEnumerable<KeyValuePair<string, string>> fields)
    {
        var signatureString = string.Join("&", fields
            .Where(field => !string.IsNullOrEmpty(field.Value))
            .Select(field => $"{field.Key}={UrlEncode(field.Value.Trim())}"));
        if (!string.IsNullOrEmpty(_options.Passphrase))
        {
            signatureString += $"&passphrase={UrlEncode(_options.Passphrase.Trim())}";
        }

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string UrlEncode(string value) => WebUtility.UrlEncode(value);
}

public sealed class PayFastOptions
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string? Passphrase { get; set; }
    public bool Sandbox { get; set; } = true;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
}