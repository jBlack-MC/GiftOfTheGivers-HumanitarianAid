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

    public bool IsEnabled => _options.Enabled;
    public bool IsSandbox => _options.Sandbox;
    public string MerchantId => _options.MerchantId;

    public string BuildCheckoutRedirectUrl(Donation donation, CheckoutUrls urls)
    {
        var baseUrl = _options.Sandbox
            ? "https://sandbox.payfast.co.za/eng/process"
            : "https://www.payfast.co.za/eng/process";

        // Field order matters: PayFast signs the fields in the order they are documented.
        var fields = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", _options.MerchantId),
            new("merchant_key", _options.MerchantKey),
            new("return_url", urls.ReturnUrl),
            new("cancel_url", urls.CancelUrl),
            new("notify_url", urls.NotifyUrl),
            new("email_address", donation.DonorEmail ?? string.Empty),
            new("m_payment_id", donation.TransactionReference ?? string.Empty),
            new("amount", donation.AmountZar.ToString("F2", CultureInfo.InvariantCulture)),
            new("item_name", Truncate($"Donation - {donation.ReliefProject?.Title ?? "General Fund"}", 100))
        };

        var signature = CreateSignature(fields);
        var query = string.Join("&", fields
            .Where(field => !string.IsNullOrEmpty(field.Value))
            .Select(field => $"{field.Key}={UrlEncode(field.Value)}"));
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

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}

public sealed class PayFastOptions
{
    /// <summary>PayFast's published public sandbox merchant id.</summary>
    public const string PublicSandboxMerchantId = "10000100";

    /// <summary>When false, no checkout redirect happens and donations stay Pending.</summary>
    public bool Enabled { get; set; } = true;

    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string? Passphrase { get; set; }
    public bool Sandbox { get; set; } = true;

    /// <summary>
    /// Optional absolute URLs. When empty they are derived from the incoming request, which is
    /// correct for any host as long as forwarded headers are configured behind a proxy.
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
}

/// <summary>
/// Fails application startup when live PayFast settings are unsafe, instead of discovering the
/// problem when a real donor tries to pay.
/// </summary>
public sealed class PayFastOptionsValidator : IValidateOptions<PayFastOptions>
{
    public ValidateOptionsResult Validate(string? name, PayFastOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.MerchantId) || string.IsNullOrWhiteSpace(options.MerchantKey))
        {
            failures.Add("PayFast:MerchantId and PayFast:MerchantKey are required when PayFast:Enabled is true.");
        }

        foreach (var (key, url) in new[]
                 {
                     ("ReturnUrl", options.ReturnUrl), ("CancelUrl", options.CancelUrl), ("NotifyUrl", options.NotifyUrl)
                 })
        {
            if (!string.IsNullOrWhiteSpace(url) && !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                failures.Add($"PayFast:{key} must be an absolute URL when set.");
            }
        }

        if (!options.Sandbox)
        {
            if (string.IsNullOrWhiteSpace(options.Passphrase))
            {
                failures.Add("PayFast:Passphrase is required for live payments; without it the ITN signature has no secret.");
            }

            if (options.MerchantId == PayFastOptions.PublicSandboxMerchantId)
            {
                failures.Add("PayFast:MerchantId is still the public sandbox merchant while PayFast:Sandbox is false.");
            }

            foreach (var url in new[] { options.ReturnUrl, options.CancelUrl, options.NotifyUrl })
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.IsLoopback || uri.Scheme != Uri.UriSchemeHttps))
                {
                    failures.Add($"PayFast URL '{url}' must be a public https URL for live payments.");
                }
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
