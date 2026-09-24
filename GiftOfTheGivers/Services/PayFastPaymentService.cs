using System.Globalization;
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

        var fields = new SortedDictionary<string, string>
        {
            ["merchant_id"] = _options.MerchantId,
            ["merchant_key"] = _options.MerchantKey,
            ["return_url"] = _options.ReturnUrl,
            ["cancel_url"] = _options.CancelUrl,
            ["notify_url"] = _options.NotifyUrl,
            ["m_payment_id"] = donation.TransactionReference ?? string.Empty,
            ["amount"] = donation.Amount.ToString("F2", CultureInfo.InvariantCulture),
            ["item_name"] = $"Donation - {donation.ReliefProject?.Title ?? "General Fund"}"
        };

        var signature = CreateSignature(fields);
        var query = string.Join("&", fields.Select(field => $"{field.Key}={Uri.EscapeDataString(field.Value)}"));
        return $"{baseUrl}?{query}&signature={signature}";
    }

    public async Task<bool> VerifyNotificationAsync(IFormCollection payload)
    {
        var fields = payload
            .Where(field => field.Key != "signature")
            .OrderBy(field => field.Key)
            .ToDictionary(field => field.Key, field => field.Value.ToString());

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
        var signatureString = string.Join("&", fields.Select(field =>
            $"{field.Key}={Uri.EscapeDataString(field.Value)}"));
        if (!string.IsNullOrEmpty(_options.Passphrase))
        {
            signatureString += $"&passphrase={Uri.EscapeDataString(_options.Passphrase)}";
        }

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
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