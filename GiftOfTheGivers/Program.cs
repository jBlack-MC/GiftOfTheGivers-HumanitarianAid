using System.Threading.RateLimiting;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using SendGrid;
using Serilog;

// QuestPDF Community licence (free for this use). Set before any document is generated.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---- Data ---------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Keys live in the database so every instance (and every restart) can read cookies and
// anti-forgery tokens issued by the others.
builder.Services.AddDataProtection()
    .SetApplicationName("GiftOfTheGivers")
    .PersistKeysToDbContext<ApplicationDbContext>();

// ---- Identity -----------------------------------------------------------
builder.Services.AddDefaultIdentity<AppUser>(options =>
    {
        // Turn on once SendGrid is configured so confirmation emails can actually be delivered.
        options.SignIn.RequireConfirmedAccount = builder.Configuration.GetValue("Identity:RequireConfirmedAccount", false);
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
        // AddDefaultIdentity limits Identity key columns to 128 characters, but every existing
        // database (and the migration snapshot) was created with the provider default of 450.
        // 0 = "not set", which keeps the runtime model identical to the schema; without this the
        // model has pending changes and startup migration fails.
        options.Stores.MaxLengthForKeys = 0;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---- Options (validated at startup) --------------------------------------
builder.Services.AddOptions<PayFastOptions>().Bind(builder.Configuration.GetSection("PayFast")).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PayFastOptions>, PayFastOptionsValidator>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<OrganisationOptions>(builder.Configuration.GetSection("Organisation"));
builder.Services.Configure<CurrencyOptions>(builder.Configuration.GetSection("Currency"));
builder.Services.Configure<StaffAccessOptions>(builder.Configuration.GetSection("StaffAccess"));
builder.Services.Configure<CachingOptions>(builder.Configuration.GetSection("Caching"));
builder.Services.AddMemoryCache();

// ---- Application services ----------------------------------------------
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddSingleton<ICurrencyConverter, CurrencyConverter>();
builder.Services.AddHttpClient<IPaymentGateway, PayFastPaymentService>(client => client.Timeout = TimeSpan.FromSeconds(15));

var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"];
if (!string.IsNullOrWhiteSpace(sendGridApiKey))
{
    builder.Services.AddSingleton<ISendGridClient>(_ => new SendGridClient(sendGridApiKey));
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, NoOpEmailService>();
}
builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();

// ---- Abuse protection ----------------------------------------------------
// Every POST except the PayFast webhook is limited per client IP. GETs are not limited.
var postPermitLimit = builder.Configuration.GetValue("RateLimiting:PostPermitLimit", 20);
var postWindowSeconds = builder.Configuration.GetValue("RateLimiting:PostWindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            context.Request.Path.StartsWithSegments("/Payments/Notify"))
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = postPermitLimit,
            Window = TimeSpan.FromSeconds(postWindowSeconds),
            QueueLimit = 0
        });
    });
});

// Behind Azure App Service / a reverse proxy, opt in so Request.Scheme and client IPs are correct.
if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Environment);
}

// ---- HTTP pipeline ------------------------------------------------------
if (app.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// /health/live: process is up. /health: also checks the database.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

/// <summary>Entry point marker so integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
