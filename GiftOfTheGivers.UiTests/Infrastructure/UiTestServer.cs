using GiftOfTheGivers.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GiftOfTheGivers.UiTests.Infrastructure;

/// <summary>
/// The site the browser talks to. By default the real application is started on a free local
/// port with Kestrel, an in-memory database and the demo accounts, so the UI tests need nothing
/// installed except a browser. Set <c>UI_BASE_URL</c> to point the same tests at a deployed
/// site instead (e.g. the Azure App Service after a release).
/// </summary>
public sealed class UiTestServer : IAsyncLifetime
{
    private SiteFactory? _factory;

    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>True when the tests run against the self-hosted copy (demo data guaranteed).</summary>
    public bool IsSelfHosted => _factory is not null;

    public Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("UI_BASE_URL");
        if (!string.IsNullOrWhiteSpace(external))
        {
            BaseUrl = external.TrimEnd('/');
            return Task.CompletedTask;
        }

        _factory = new SiteFactory();
        _factory.UseKestrel(0);          // 0 = any free port
        _factory.StartServer();
        BaseUrl = _factory.ClientOptions.BaseAddress.ToString().TrimEnd('/');
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    private sealed class SiteFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = "gotg-ui-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Seed:DemoData", "true");
            // No payment provider in UI tests: the donation goes straight to the receipt page.
            builder.UseSetting("PayFast:Enabled", "false");
            builder.UseSetting("RateLimiting:PostPermitLimit", "1000");
            builder.UseSetting("Caching:PublicPageSeconds", "0");

            builder.ConfigureTestServices(services =>
            {
                foreach (var descriptor in services.Where(d =>
                             d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                             d.ServiceType == typeof(DbContextOptions) ||
                             (d.ServiceType.IsGenericType &&
                              d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                         .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options => options
                    .UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });
        }
    }
}

[CollectionDefinition(Name)]
public sealed class UiCollection : ICollectionFixture<UiTestServer>
{
    public const string Name = "UI";
}
