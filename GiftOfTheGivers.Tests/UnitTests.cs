using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Tests;

[Trait("Category", "Unit")]
public class CurrencyConverterTests
{
    private static CurrencyConverter Create(Dictionary<string, decimal> rates) =>
        new(Options.Create(new CurrencyOptions { RatesToZar = new(rates, StringComparer.OrdinalIgnoreCase) }));

    [Fact]
    public void ToZar_ConvertsAndRoundsToCents()
    {
        var converter = Create(new() { ["USD"] = 18.333333m, ["EUR"] = 20m });

        Assert.Equal((100m, 1m), converter.ToZar(100m, "ZAR"));
        Assert.Equal(183.33m, converter.ToZar(10m, "USD").AmountZar);
    }

    [Fact]
    public void Constructor_FailsFastWhenASupportedCurrencyHasNoRate()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Create(new() { ["USD"] = 18m }));

        Assert.Contains("EUR", exception.Message);
    }

    [Fact]
    public void ToZar_RejectsUnsupportedCurrency()
    {
        var converter = Create(new() { ["USD"] = 18m, ["EUR"] = 20m });

        Assert.Throws<ArgumentException>(() => converter.ToZar(1m, "GBP"));
    }
}

[Trait("Category", "Unit")]
public class CsvTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"http://evil\")", "\"'=HYPERLINK(\"\"http://evil\"\")\"")]
    [InlineData("+27 82 000 0000", "'+27 82 000 0000")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData(null, "")]
    public void Escape_NeutralisesFormulasAndQuotesSeparators(string? input, string expected)
    {
        Assert.Equal(expected, Csv.Escape(input));
    }
}

[Trait("Category", "Unit")]
public class MoneyTests
{
    [Theory]
    [InlineData(1234.5, "ZAR", "R 1,234.50")]
    [InlineData(75, "USD", "$ 75.00")]
    [InlineData(10, "EUR", "€ 10.00")]
    public void Format_IsCultureIndependent(decimal amount, string currency, string expected)
    {
        Assert.Equal(expected, Money.Format(amount, currency));
    }
}

[Trait("Category", "Unit")]
public class PayFastOptionsValidatorTests
{
    private static readonly PayFastOptionsValidator Validator = new();

    [Fact]
    public void Sandbox_WithPublicCredentials_IsValid()
    {
        var result = Validator.Validate(null, new PayFastOptions
        {
            MerchantId = PayFastOptions.PublicSandboxMerchantId, MerchantKey = "key", Sandbox = true
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Live_WithoutPassphraseOrWithSandboxMerchantOrLocalhost_FailsStartup()
    {
        var result = Validator.Validate(null, new PayFastOptions
        {
            MerchantId = PayFastOptions.PublicSandboxMerchantId,
            MerchantKey = "key",
            Sandbox = false,
            NotifyUrl = "https://localhost:7052/Payments/Notify"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Passphrase"));
        Assert.Contains(result.Failures!, f => f.Contains("sandbox merchant"));
        Assert.Contains(result.Failures!, f => f.Contains("public https"));
    }

    [Fact]
    public void Disabled_IsAlwaysValid()
    {
        Assert.True(Validator.Validate(null, new PayFastOptions { Enabled = false, Sandbox = false }).Succeeded);
    }
}

[Trait("Category", "Unit")]
public class PayFastCheckoutTests
{
    [Fact]
    public void BuildCheckoutRedirectUrl_ChargesTheRandAmountAndCarriesTheReference()
    {
        var service = new PayFastPaymentService(
            Options.Create(new PayFastOptions { MerchantId = "10000100", MerchantKey = "46f0cd694581a", Sandbox = true }),
            new HttpClient());

        var url = service.BuildCheckoutRedirectUrl(
            new Donation { Amount = 10m, Currency = "USD", AmountZar = 185m, TransactionReference = "abc123" },
            new CheckoutUrls("https://site/return?reference=abc123", "https://site/cancel", "https://site/notify"));

        Assert.StartsWith("https://sandbox.payfast.co.za/eng/process?", url);
        Assert.Contains("amount=185.00", url);
        Assert.Contains("m_payment_id=abc123", url);
        Assert.Matches("signature=[0-9a-f]{32}$", url);
    }
}

[Trait("Category", "Unit")]
public class TaxCertificateIssuerTests
{
    [Fact]
    public void Issuer_IsOfficialOnlyWhenNotDemoAndPboConfigured()
    {
        Assert.False(CertificateIssuer.From(new OrganisationOptions { IsDemo = true, PboReference = "930000000" }).IsOfficial);
        Assert.False(CertificateIssuer.From(new OrganisationOptions { IsDemo = false, PboReference = "" }).IsOfficial);
        Assert.True(CertificateIssuer.From(new OrganisationOptions { IsDemo = false, PboReference = "930000000" }).IsOfficial);
    }

    [Fact]
    public void DonorDisplayName_HidesAnonymousDonors()
    {
        Assert.Equal("Anonymous Donor", TaxCertificatePdf.DonorDisplayName(new Donation { IsAnonymous = true, DonorName = "Hidden" }));
        Assert.Equal("Named", TaxCertificatePdf.DonorDisplayName(new Donation { DonorName = "Named" }));
    }
}
