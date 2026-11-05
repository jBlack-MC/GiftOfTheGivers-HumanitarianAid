extern alias functions;

using System.Net;
using System.Security.Claims;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using functions::GiftOfTheGivers.Functions;

namespace GiftOfTheGivers.Tests;

/// <summary>
/// Integration of the three parts of the certificate workflow: the web donation form writes the
/// donation, the PayFast notification verifies it, and the Azure Function reads the same
/// database to issue the PDF. The function runs in-process against the web app's database.
/// </summary>
[Trait("Category", "Integration")]
public class TaxCertificateFunctionTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public TaxCertificateFunctionTests(TestApplicationFactory factory) => _factory = factory;

    private async Task<HttpResponseData> CallFunctionAsync(string reference)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var function = new GenerateTaxCertificateFunction(
            context,
            Options.Create(new OrganisationOptions { Name = "Gift of the Givers Foundation", IsDemo = true }),
            NullLogger<GenerateTaxCertificateFunction>.Instance);

        var request = new FakeHttpRequestData(new FakeFunctionContext(), new Uri($"http://localhost/api/tax-certificate/{reference}"));
        return await function.GenerateTaxCertificate(request, reference);
    }

    private static async Task<string> BodyTextAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        return await new StreamReader(response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task DonationForm_ThenPaymentNotification_ThenFunction_ReturnsCertificatePdf()
    {
        // 1. Donor submits the web form (anonymous, monthly, in US dollars).
        var token = Guid.NewGuid();
        var form = new Dictionary<string, string>
        {
            ["IdempotencyToken"] = token.ToString(),
            ["DonationType"] = "Recurring",
            ["Currency"] = "USD",
            ["Amount"] = "20",
            ["DonorName"] = "Anonymous Friend",
            ["DonorEmail"] = "friend@test.local",
            ["PaymentMethod"] = "Card",
            ["IsAnonymous"] = "true",
            ["PrivacyConsent"] = "true"
        };
        var submitted = await TestApplicationFactory.PostFormAsync(_factory.CreateClientFor(), "/Home/Donate", "/Home/Donate", form);
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        var donation = await _factory.WithDbAsync(db => db.Donations.AsNoTracking().SingleAsync(d => d.IdempotencyKey == token));
        Assert.Equal(370m, donation.AmountZar); // 20 USD × 18.50

        // 2. Before payment is confirmed the function must refuse to issue a certificate.
        var early = await CallFunctionAsync(donation.TransactionReference!);
        Assert.Equal(HttpStatusCode.NotFound, early.StatusCode);

        // 3. PayFast confirms the payment.
        var notify = await _factory.CreateClientFor().PostAsync("/Payments/Notify", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["m_payment_id"] = donation.TransactionReference!,
            ["pf_payment_id"] = "PF-FUNC-1",
            ["payment_status"] = "COMPLETE",
            ["amount_gross"] = "370.00",
            ["merchant_id"] = "10000100",
            ["signature"] = "checked-by-fake-gateway"
        }));
        Assert.Equal(HttpStatusCode.OK, notify.StatusCode);

        // 4. The function now reads the verified donation and streams a PDF.
        var response = await CallFunctionAsync(donation.TransactionReference!);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Headers.GetValues("Content-Type").Single());
        Assert.Contains(donation.TransactionReference!, response.Headers.GetValues("Content-Disposition").Single());
        response.Body.Position = 0;
        var header = new byte[5];
        await response.Body.ReadExactlyAsync(header);
        Assert.Equal("%PDF-"u8.ToArray(), header);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("has spaces in it 1234")]
    [InlineData("../../etc/passwd/aaaaaaaa")]
    public async Task MalformedReference_IsRejectedWith400(string reference)
    {
        var response = await CallFunctionAsync(reference);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("valid donation reference", await BodyTextAsync(response));
    }

    [Fact]
    public async Task UnknownReference_Returns404()
    {
        var response = await CallFunctionAsync(Guid.NewGuid().ToString("N"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FailedPayment_NeverGetsACertificate()
    {
        var donation = await _factory.AddDonationAsync(null, PaymentStatus.Failed);

        var response = await CallFunctionAsync(donation.TransactionReference!);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- minimal Functions worker doubles ----------------------------------------------

    private sealed class FakeFunctionContext : FunctionContext
    {
        public override string InvocationId { get; } = Guid.NewGuid().ToString();
        public override string FunctionId { get; } = nameof(GenerateTaxCertificateFunction.GenerateTaxCertificate);
        public override TraceContext TraceContext => null!;
        public override BindingContext BindingContext => null!;
        public override RetryContext RetryContext => null!;
        public override IServiceProvider InstanceServices { get; set; } = new ServiceCollection().BuildServiceProvider();
        public override FunctionDefinition FunctionDefinition => null!;
        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        public override IInvocationFeatures Features => null!;
    }

    private sealed class FakeHttpRequestData(FunctionContext context, Uri url) : HttpRequestData(context)
    {
        public override Stream Body { get; } = new MemoryStream();
        public override HttpHeadersCollection Headers { get; } = [];
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];
        public override Uri Url { get; } = url;
        public override IEnumerable<ClaimsIdentity> Identities { get; } = [];
        public override string Method => "GET";
        public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
    }

    private sealed class FakeHttpResponseData(FunctionContext context) : HttpResponseData(context)
    {
        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; } = [];
        public override Stream Body { get; set; } = new MemoryStream();
        public override HttpCookies Cookies => null!;
    }
}
