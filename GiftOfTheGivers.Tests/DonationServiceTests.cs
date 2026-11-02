using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using GiftOfTheGivers.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GiftOfTheGivers.Tests;

/// <summary>
/// Business rules of <see cref="DonationService"/> in isolation: the currency converter and
/// the audit trail are replaced with stubs so each test controls rates and can inspect what
/// would have been audited, and the database is a private in-memory store.
/// </summary>
[Trait("Category", "Unit")]
public class DonationServiceTests
{
    private const string MerchantId = "10000100";

    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase("donation-service-" + Guid.NewGuid())
        .Options);

    private readonly StubCurrencyConverter _converter = new();
    private readonly RecordingAuditService _audit = new();

    private DonationService CreateService() =>
        new(_context, _converter, _audit, NullLogger<DonationService>.Instance);

    private async Task<ReliefProject> AddProjectAsync(ProjectStatus status = ProjectStatus.Active, decimal raised = 0m)
    {
        var project = new ReliefProject
        {
            Title = "Flood relief",
            Description = "Test",
            Location = "Durban",
            Status = status,
            FundsRequired = 10_000m,
            FundsRaised = raised,
            OfflineFundsZar = raised,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        _context.ReliefProjects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    private static DonateViewModel Form(int? projectId = null, decimal amount = 100m, string currency = "ZAR",
        string type = "OneTime", bool anonymous = false) => new()
    {
        ReliefProjectId = projectId,
        Amount = amount,
        Currency = currency,
        DonationType = type,
        DonorName = "  Lerato Dlamini ",
        DonorEmail = " lerato@test.local ",
        PaymentMethod = "Card",
        IsAnonymous = anonymous,
        PrivacyConsent = true
    };

    [Fact]
    public async Task Capture_ConvertsWithTheInjectedRate_AndStoresPendingDonation()
    {
        _converter.Rates["USD"] = 18m;
        var form = Form(amount: 10m, currency: "USD", type: "Recurring", anonymous: true);

        var result = await CreateService().CaptureAsync(form, donorUserId: null);

        Assert.Equal(CaptureOutcome.Created, result.Outcome);
        var donation = result.Donation!;
        Assert.Equal(180m, donation.AmountZar);
        Assert.Equal(18m, donation.ExchangeRateToZar);
        Assert.Equal("Recurring", donation.DonationType);
        Assert.True(donation.IsAnonymous);
        Assert.Null(donation.DonorId);
        Assert.Equal(PaymentStatus.Pending, donation.PaymentStatus);
        Assert.Equal("Lerato Dlamini", donation.DonorName);
        Assert.Equal(32, donation.TransactionReference!.Length);
        Assert.Equal(("USD", 10m), _converter.Calls.Single());
    }

    [Fact]
    public async Task Capture_SameIdempotencyToken_ReturnsTheFirstDonation()
    {
        var service = CreateService();
        var form = Form();

        var first = await service.CaptureAsync(form, null);
        var second = await service.CaptureAsync(form, null);

        Assert.Equal(CaptureOutcome.AlreadySubmitted, second.Outcome);
        Assert.Equal(first.Donation!.Id, second.Donation!.Id);
        Assert.Equal(1, await _context.Donations.CountAsync());
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Suspended)]
    public async Task Capture_ProjectNotActive_IsRefused(ProjectStatus status)
    {
        var project = await AddProjectAsync(status);

        var result = await CreateService().CaptureAsync(Form(project.Id), null);

        Assert.Equal(CaptureOutcome.ProjectUnavailable, result.Outcome);
        Assert.Empty(_context.Donations);
    }

    [Fact]
    public async Task Capture_NeverChangesTheProjectTotal()
    {
        var project = await AddProjectAsync(raised: 500m);

        await CreateService().CaptureAsync(Form(project.Id, 250m), null);

        Assert.Equal(500m, (await _context.ReliefProjects.SingleAsync()).FundsRaised);
    }

    [Fact]
    public async Task CompleteNotification_VerifiesDonation_AddsToTotal_AndAudits()
    {
        var project = await AddProjectAsync(raised: 1_000m);
        var service = CreateService();
        var donation = (await service.CaptureAsync(Form(project.Id, 250m), "donor-1")).Donation!;

        var (outcome, _) = await service.ApplyPaymentNotificationAsync(
            new PaymentNotification(donation.TransactionReference!, "COMPLETE", "250.00", MerchantId, "PF-1"), MerchantId);

        Assert.Equal(NotificationOutcome.Verified, outcome);
        Assert.Equal(1_250m, project.FundsRaised);
        Assert.True(donation.TaxCertificateIssued);
        Assert.Equal("PaymentVerified", _audit.Entries.Single().Action);
    }

    [Fact]
    public async Task ReplayedNotification_DoesNotCountTheMoneyTwice()
    {
        var project = await AddProjectAsync();
        var service = CreateService();
        var donation = (await service.CaptureAsync(Form(project.Id, 100m), null)).Donation!;
        var itn = new PaymentNotification(donation.TransactionReference!, "COMPLETE", "100.00", MerchantId, "PF-2");

        await service.ApplyPaymentNotificationAsync(itn, MerchantId);
        var (replay, _) = await service.ApplyPaymentNotificationAsync(itn, MerchantId);

        Assert.Equal(NotificationOutcome.AlreadyVerified, replay);
        Assert.Equal(100m, project.FundsRaised);
    }

    [Theory]
    [InlineData("COMPLETE", "99.99", MerchantId, NotificationOutcome.AmountMismatch)]
    [InlineData("COMPLETE", "100.00", "99999999", NotificationOutcome.MerchantMismatch)]
    [InlineData("CANCELLED", "100.00", MerchantId, NotificationOutcome.MarkedFailed)]
    public async Task BadOrCancelledNotification_NeverVerifies(string status, string amount, string merchant,
        NotificationOutcome expected)
    {
        var project = await AddProjectAsync();
        var service = CreateService();
        var donation = (await service.CaptureAsync(Form(project.Id, 100m), null)).Donation!;

        var (outcome, _) = await service.ApplyPaymentNotificationAsync(
            new PaymentNotification(donation.TransactionReference!, status, amount, merchant, "PF-3"), MerchantId);

        Assert.Equal(expected, outcome);
        Assert.NotEqual(PaymentStatus.Verified, donation.PaymentStatus);
        Assert.Equal(0m, project.FundsRaised);
    }

    [Fact]
    public async Task UnknownReference_IsReported()
    {
        var (outcome, donation) = await CreateService().ApplyPaymentNotificationAsync(
            new PaymentNotification("does-not-exist", "COMPLETE", "1.00", MerchantId, null), MerchantId);

        Assert.Equal(NotificationOutcome.UnknownReference, outcome);
        Assert.Null(donation);
    }

    [Fact]
    public async Task Recalculate_SetsTotalToOfflineFundsPlusVerifiedDonations()
    {
        var project = await AddProjectAsync(raised: 300m);
        project.FundsRaised = 9_999m; // drifted figure
        _context.Donations.AddRange(
            new Donation { ReliefProjectId = project.Id, Amount = 100m, AmountZar = 100m, PaymentStatus = PaymentStatus.Verified },
            new Donation { ReliefProjectId = project.Id, Amount = 50m, AmountZar = 50m, PaymentStatus = PaymentStatus.Pending });
        await _context.SaveChangesAsync();

        var (before, after) = await CreateService().RecalculateFundsRaisedAsync(project.Id);

        Assert.Equal(9_999m, before);
        Assert.Equal(400m, after);
    }

    /// <summary>Stub: fixed rates chosen by the test, and a record of every conversion asked for.</summary>
    private sealed class StubCurrencyConverter : ICurrencyConverter
    {
        public Dictionary<string, decimal> Rates { get; } = new(StringComparer.OrdinalIgnoreCase) { ["ZAR"] = 1m };
        public List<(string Currency, decimal Amount)> Calls { get; } = [];
        public IReadOnlyDictionary<string, decimal> RatesToZar => Rates;

        public (decimal AmountZar, decimal Rate) ToZar(decimal amount, string currency)
        {
            Calls.Add((currency, amount));
            var rate = Rates[currency];
            return (Math.Round(amount * rate, 2), rate);
        }
    }

    /// <summary>Mock audit trail: keeps entries in memory so tests can assert on them.</summary>
    private sealed class RecordingAuditService : IAuditService
    {
        public List<(string UserId, string Action, string Entity, int Id)> Entries { get; } = [];

        public void Record(string userId, string action, string targetEntity, int targetId, string? details = null) =>
            Entries.Add((userId, action, targetEntity, targetId));

        public Task LogAsync(string userId, string action, string targetEntity, int targetId, string? details = null)
        {
            Record(userId, action, targetEntity, targetId, details);
            return Task.CompletedTask;
        }
    }
}
