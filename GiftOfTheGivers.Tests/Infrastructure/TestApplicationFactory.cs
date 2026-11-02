using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Tests.Infrastructure;

/// <summary>
/// Hosts the real application (routing, filters, authorization, views) in memory, with an
/// isolated in-memory database per factory, a fake payment gateway and header-based test users.
/// </summary>
public class TestApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserHeader = "X-Test-User";

    private readonly string _databaseName = "gotg-tests-" + Guid.NewGuid();

    /// <summary>Extra configuration applied on top of appsettings.json.</summary>
    protected virtual Dictionary<string, string?> Settings => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused",
        ["Database:MigrateOnStartup"] = "false",
        ["Seed:DemoData"] = "false",
        ["RateLimiting:PostPermitLimit"] = "1000",
        ["PayFast:Enabled"] = "true",
        ["PayFast:Sandbox"] = "true"
    };

    public FakePaymentGateway PaymentGateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            // Swap SQL Server for an isolated in-memory database.
            var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();
            foreach (var descriptor in dbDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(PaymentGateway);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    public HttpClient CreateClientFor(string? userId = null, params string[] roles)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add(TestUserHeader, $"{userId};{string.Join(",", roles)}");
        }

        return client;
    }

    public async Task WithDbAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(context);
    }

    public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    public async Task<T> WithServicesAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public async Task<ReliefProject> AddProjectAsync(
        string title = "Test Project", ProjectStatus status = ProjectStatus.Active, decimal offline = 0m)
    {
        return await WithDbAsync(async db =>
        {
            var project = new ReliefProject
            {
                Title = title,
                Description = "Test description",
                Location = "Cape Town",
                StartDate = DateTime.UtcNow.Date,
                Status = status,
                FundsRequired = 100_000m,
                OfflineFundsZar = offline,
                FundsRaised = offline,
                RowVersion = Guid.NewGuid().ToByteArray()
            };
            db.ReliefProjects.Add(project);
            await db.SaveChangesAsync();
            return project;
        });
    }

    public async Task<Donation> AddDonationAsync(
        int? projectId, PaymentStatus status, decimal amountZar = 250m, string? donorId = null)
    {
        return await WithDbAsync(async db =>
        {
            var donation = new Donation
            {
                ReliefProjectId = projectId,
                Amount = amountZar,
                AmountZar = amountZar,
                Currency = "ZAR",
                DonorId = donorId,
                DonorName = "Test Donor",
                DonorEmail = "donor@test.local",
                PaymentStatus = status,
                TransactionReference = Guid.NewGuid().ToString("N"),
                TaxCertificateIssued = status == PaymentStatus.Verified
            };
            db.Donations.Add(donation);
            await db.SaveChangesAsync();
            return donation;
        });
    }

    /// <summary>GETs a page and returns the anti-forgery token rendered in its form.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success)
        {
            throw new InvalidOperationException($"No anti-forgery token on {url}.");
        }

        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string formPageUrl, string postUrl, IDictionary<string, string> fields)
    {
        var token = await GetAntiforgeryTokenAsync(client, formPageUrl);
        var content = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(content));
    }
}

/// <summary>Authenticates requests from the <c>X-Test-User: userId;Role1,Role2</c> header.</summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestApplicationFactory.TestUserHeader, out var header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var parts = header.ToString().Split(';', 2);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, parts[0]),
            new(ClaimTypes.Name, parts[0] + "@test.local")
        };
        if (parts.Length > 1)
        {
            claims.AddRange(parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Payment gateway double: records checkouts and accepts every ITN signature.</summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public bool IsEnabled { get; set; } = true;
    public bool IsSandbox => true;
    public string MerchantId => PayFastOptions.PublicSandboxMerchantId;
    public List<(Donation Donation, CheckoutUrls Urls)> Checkouts { get; } = [];

    public string BuildCheckoutRedirectUrl(Donation donation, CheckoutUrls urls)
    {
        Checkouts.Add((donation, urls));
        return $"https://sandbox.payfast.test/eng/process?m_payment_id={donation.TransactionReference}";
    }

    public Task<bool> VerifyNotificationAsync(IFormCollection payload) => Task.FromResult(true);
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
