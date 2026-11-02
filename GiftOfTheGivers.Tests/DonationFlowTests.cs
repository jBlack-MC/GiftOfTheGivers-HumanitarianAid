using System.Net;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Tests;

/// <summary>Audit findings R3–R7 and R16: donation capture, payment verification and certificates.</summary>
public class DonationFlowTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public DonationFlowTests(TestApplicationFactory factory) => _factory = factory;

    private static Dictionary<string, string> DonationForm(int? projectId, decimal amount = 500m, string currency = "ZAR",
        Guid? token = null) => new()
    {
        ["IdempotencyToken"] = (token ?? Guid.NewGuid()).ToString(),
        ["DonationType"] = "OneTime",
        ["ReliefProjectId"] = projectId?.ToString() ?? string.Empty,
        ["Currency"] = currency,
        ["Amount"] = amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["DonorName"] = "Thandi Test",
        ["DonorEmail"] = "thandi@test.local",
        ["PaymentMethod"] = "Card",
        ["PrivacyConsent"] = "true"
    };

    private Task<HttpResponseMessage> PostDonationAsync(HttpClient client, Dictionary<string, string> form) =>
        TestApplicationFactory.PostFormAsync(client, "/Home/Donate", "/Home/Donate", form);

    private async Task<HttpResponseMessage> PostNotificationAsync(Donation donation, string status, string amount,
        string merchantId = "10000100")
    {
        var client = _factory.CreateClientFor();
        return await client.PostAsync("/Payments/Notify", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["m_payment_id"] = donation.TransactionReference!,
            ["pf_payment_id"] = "PF-" + donation.Id,
            ["payment_status"] = status,
            ["amount_gross"] = amount,
            ["merchant_id"] = merchantId,
            ["signature"] = "checked-by-fake-gateway"
        }));
    }

    private Task<ReliefProject> ReloadProjectAsync(int id) =>
        _factory.WithDbAsync(db => db.ReliefProjects.AsNoTracking().SingleAsync(p => p.Id == id));

    private Task<Donation> ReloadDonationAsync(int id) =>
        _factory.WithDbAsync(db => db.Donations.AsNoTracking().SingleAsync(d => d.Id == id));

    [Fact]
    public async Task Donate_RecordsPendingDonation_RedirectsToPayFast_AndDoesNotTouchProjectTotal()
    {
        var project = await _factory.AddProjectAsync(offline: 1000m);
        var form = DonationForm(project.Id, 500m);

        var response = await PostDonationAsync(_factory.CreateClientFor(), form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://sandbox.payfast.test/", response.Headers.Location!.ToString());
        var donation = await _factory.WithDbAsync(db =>
            db.Donations.AsNoTracking().SingleAsync(d => d.IdempotencyKey == Guid.Parse(form["IdempotencyToken"])));
        Assert.Equal(PaymentStatus.Pending, donation.PaymentStatus);
        Assert.Equal(500m, donation.AmountZar);
        Assert.NotNull(donation.PrivacyConsentAt);
        Assert.Equal(1000m, (await ReloadProjectAsync(project.Id)).FundsRaised);
    }

    [Fact]
    public async Task Donate_ForeignCurrency_IsChargedInRandAtConfiguredRate()
    {
        var form = DonationForm(null, 10m, "USD");

        await PostDonationAsync(_factory.CreateClientFor(), form);

        var donation = await _factory.WithDbAsync(db =>
            db.Donations.AsNoTracking().SingleAsync(d => d.IdempotencyKey == Guid.Parse(form["IdempotencyToken"])));
        Assert.Equal(18.50m, donation.ExchangeRateToZar);
        Assert.Equal(185.00m, donation.AmountZar);
    }

    [Theory]
    [InlineData("Currency", "GBP")]
    [InlineData("DonationType", "Weekly")]
    [InlineData("PaymentMethod", "PayPal")]
    [InlineData("Amount", "99999999999999")]
    [InlineData("PrivacyConsent", "false")]
    public async Task Donate_InvalidInput_RedisplaysFormInsteadOfServerError(string field, string value)
    {
        var form = DonationForm(null);
        form[field] = value;

        var response = await PostDonationAsync(_factory.CreateClientFor(), form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        Assert.False(await _factory.WithDbAsync(db =>
            db.Donations.AnyAsync(d => d.IdempotencyKey == Guid.Parse(form["IdempotencyToken"]))));
    }

    [Fact]
    public async Task Donate_ToSuspendedProject_IsRejectedWithMessage()
    {
        var project = await _factory.AddProjectAsync("Paused", ProjectStatus.Suspended);

        var response = await PostDonationAsync(_factory.CreateClientFor(), DonationForm(project.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not accepting donations", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Donate_SameIdempotencyTokenTwice_CreatesOneDonation()
    {
        var client = _factory.CreateClientFor();
        var form = DonationForm(null);

        await PostDonationAsync(client, form);
        await PostDonationAsync(client, form);

        Assert.Equal(1, await _factory.WithDbAsync(db =>
            db.Donations.CountAsync(d => d.IdempotencyKey == Guid.Parse(form["IdempotencyToken"]))));
    }

    [Fact]
    public async Task Donate_WhenPaymentsDisabled_GoesStraightToReceiptShowingAwaitingPayment()
    {
        var factory = new TestApplicationFactory();
        factory.PaymentGateway.IsEnabled = false;
        var client = factory.CreateClientFor();

        var response = await PostDonationAsync(client, DonationForm(null));
        var receipt = await client.GetStringAsync(response.Headers.Location!.ToString());

        Assert.Contains("/Donate/Confirmation?reference=", response.Headers.Location!.ToString());
        Assert.Contains("Awaiting payment", receipt);
    }

    [Fact]
    public async Task Notify_Complete_VerifiesOnceAndAddsRandAmountOnce_EvenIfReplayed()
    {
        var project = await _factory.AddProjectAsync(offline: 100m);
        var donation = await _factory.AddDonationAsync(project.Id, PaymentStatus.Pending, 250m);

        var first = await PostNotificationAsync(donation, "COMPLETE", "250.00");
        var replay = await PostNotificationAsync(donation, "COMPLETE", "250.00");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var saved = await ReloadDonationAsync(donation.Id);
        Assert.Equal(PaymentStatus.Verified, saved.PaymentStatus);
        Assert.Equal("PF-" + donation.Id, saved.ProviderPaymentId);
        Assert.True(saved.TaxCertificateIssued);
        Assert.Equal(350m, (await ReloadProjectAsync(project.Id)).FundsRaised);
        Assert.True(await _factory.WithDbAsync(db =>
            db.AuditLogs.AnyAsync(a => a.Action == "PaymentVerified" && a.TargetId == donation.Id)));
    }

    [Fact]
    public async Task Notify_AmountMismatch_IsRefusedAndDonationStaysPending()
    {
        var project = await _factory.AddProjectAsync();
        var donation = await _factory.AddDonationAsync(project.Id, PaymentStatus.Pending, 250m);

        var response = await PostNotificationAsync(donation, "COMPLETE", "1.00");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PaymentStatus.Pending, (await ReloadDonationAsync(donation.Id)).PaymentStatus);
        Assert.Equal(0m, (await ReloadProjectAsync(project.Id)).FundsRaised);
    }

    [Fact]
    public async Task Notify_WrongMerchant_IsRefused()
    {
        var donation = await _factory.AddDonationAsync(null, PaymentStatus.Pending, 250m);

        var response = await PostNotificationAsync(donation, "COMPLETE", "250.00", merchantId: "99999999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PaymentStatus.Pending, (await ReloadDonationAsync(donation.Id)).PaymentStatus);
    }

    [Fact]
    public async Task Notify_Failed_MarksDonationFailedWithoutChangingTotals()
    {
        var project = await _factory.AddProjectAsync(offline: 40m);
        var donation = await _factory.AddDonationAsync(project.Id, PaymentStatus.Pending, 250m);

        var response = await PostNotificationAsync(donation, "FAILED", "250.00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Failed, (await ReloadDonationAsync(donation.Id)).PaymentStatus);
        Assert.Equal(40m, (await ReloadProjectAsync(project.Id)).FundsRaised);
    }

    [Fact]
    public async Task Notify_UnknownReference_IsNotFound()
    {
        var response = await PostNotificationAsync(
            new Donation { Id = 0, TransactionReference = "does-not-exist" }, "COMPLETE", "1.00");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Certificates_OnlyForOwnersOfVerifiedDonations()
    {
        var pending = await _factory.AddDonationAsync(null, PaymentStatus.Pending, donorId: "owner-1");
        var verified = await _factory.AddDonationAsync(null, PaymentStatus.Verified, donorId: "owner-1");
        var owner = _factory.CreateClientFor("owner-1", "Donor");
        var stranger = _factory.CreateClientFor("stranger-1", "Donor");

        var pdf = await owner.GetAsync($"/Donor/DownloadTaxCertificate/{verified.Id}");

        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/Donor/DownloadTaxCertificate/{pending.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/Donor/DownloadTaxCertificate/{verified.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/Donor/TaxCertificate/{verified.Id}")).StatusCode);
    }

    [Fact]
    public async Task LegacyCertificateRoute_RedirectsToDonorPortal()
    {
        var response = await _factory.CreateClientFor("owner-2", "Donor").GetAsync("/Donate/Tax-Certificate/5");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Donor/TaxCertificate/5", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Receipt_IsFoundByReferenceNotSequentialId()
    {
        var donation = await _factory.AddDonationAsync(null, PaymentStatus.Verified);
        var client = _factory.CreateClientFor();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Donate/Confirmation?reference={donation.TransactionReference}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Donate/Confirmation?reference={donation.Id}")).StatusCode);
    }

    [Fact]
    public async Task FundingReconciliation_RecalculateUsesOfflinePlusVerifiedOnly()
    {
        var project = await _factory.AddProjectAsync("Inflated", offline: 1000m);
        await _factory.AddDonationAsync(project.Id, PaymentStatus.Verified, 200m);
        await _factory.AddDonationAsync(project.Id, PaymentStatus.Pending, 9999m);
        await _factory.WithDbAsync(async db =>
        {
            var tracked = await db.ReliefProjects.SingleAsync(p => p.Id == project.Id);
            tracked.FundsRaised = 50_000m; // inflated by the pre-fix behaviour
            await db.SaveChangesAsync();
        });
        var client = _factory.CreateClientFor("employee-3", "Employee");

        var response = await TestApplicationFactory.PostFormAsync(client, "/Employee/FundingReconciliation",
            "/Employee/RecalculateFunding", new Dictionary<string, string> { ["id"] = project.Id.ToString() });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(1200m, (await ReloadProjectAsync(project.Id)).FundsRaised);
    }

    [Fact]
    public async Task EditingOfflineFunds_AdjustsTotalByTheDifference()
    {
        var project = await _factory.AddProjectAsync("Offline edit", offline: 100m);
        await _factory.WithDbAsync(async db =>
        {
            (await db.ReliefProjects.SingleAsync(p => p.Id == project.Id)).FundsRaised = 350m; // 100 offline + 250 verified
            await db.SaveChangesAsync();
        });
        var client = _factory.CreateClientFor("employee-4", "Employee");

        var response = await TestApplicationFactory.PostFormAsync(client, $"/Employee/EditReliefProject/{project.Id}",
            "/Employee/EditReliefProject", new Dictionary<string, string>
            {
                ["Id"] = project.Id.ToString(),
                ["Title"] = "Offline edit",
                ["Description"] = "Test description",
                ["Location"] = "Cape Town",
                ["Status"] = "Active",
                ["StartDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["FundsRequired"] = "100000",
                ["OfflineFundsZar"] = "400"
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var saved = await ReloadProjectAsync(project.Id);
        Assert.Equal(400m, saved.OfflineFundsZar);
        Assert.Equal(650m, saved.FundsRaised);
    }
}
