using System.Net;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Tests;

/// <summary>Volunteer/contact forms (consent, no invented data), live stats and rate limiting.</summary>
[Trait("Category", "Integration")]
public class PublicFormsTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public PublicFormsTests(TestApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Volunteer_WithoutDateOfBirth_StoresNullInsteadOfInventingOne()
    {
        var email = $"vol-{Guid.NewGuid():N}@test.local";

        var response = await TestApplicationFactory.PostFormAsync(_factory.CreateClientFor(), "/Home/Volunteer", "/Home/Volunteer",
            new Dictionary<string, string>
            {
                ["FullName"] = "Ayanda Volunteer",
                ["Email"] = email,
                ["Skills"] = "First aid",
                ["Availability"] = "Weekends",
                ["PrivacyConsent"] = "true"
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var volunteer = await _factory.WithDbAsync(db => db.Volunteers.AsNoTracking().SingleAsync(v => v.Email == email));
        Assert.Null(volunteer.DateOfBirth);
        Assert.Null(volunteer.PhoneNumber);
        Assert.NotNull(volunteer.PrivacyConsentAt);
    }

    [Fact]
    public async Task Volunteer_WithoutConsent_IsNotSaved()
    {
        var email = $"vol-{Guid.NewGuid():N}@test.local";

        var response = await TestApplicationFactory.PostFormAsync(_factory.CreateClientFor(), "/Home/Volunteer", "/Home/Volunteer",
            new Dictionary<string, string>
            {
                ["FullName"] = "No Consent",
                ["Email"] = email,
                ["Skills"] = "Driving",
                ["Availability"] = "Anytime",
                ["PrivacyConsent"] = "false"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("privacy notice", await response.Content.ReadAsStringAsync());
        Assert.False(await _factory.WithDbAsync(db => db.Volunteers.AnyAsync(v => v.Email == email)));
    }

    [Fact]
    public async Task Contact_RecordsConsentTimestamp()
    {
        var email = $"contact-{Guid.NewGuid():N}@test.local";

        var response = await TestApplicationFactory.PostFormAsync(_factory.CreateClientFor(), "/Home/Contact", "/Home/Contact",
            new Dictionary<string, string>
            {
                ["Name"] = "Lerato",
                ["Email"] = email,
                ["Subject"] = "Question",
                ["Message"] = "Hello",
                ["PrivacyConsent"] = "true"
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var inquiry = await _factory.WithDbAsync(db => db.Inquiries.AsNoTracking().SingleAsync(i => i.Email == email));
        Assert.NotNull(inquiry.PrivacyConsentAt);
    }

    [Fact]
    public async Task HomePage_ShowsLiveFiguresInsteadOfHardCodedStatistics()
    {
        var html = await _factory.CreateClientFor().GetStringAsync("/");

        Assert.DoesNotContain("50,000+", html);
        Assert.Contains("Live figures from this system", html);
    }

    [Fact]
    public async Task PrivacyPage_IsARealNoticeNotTheTemplate()
    {
        var html = await _factory.CreateClientFor().GetStringAsync("/Home/Privacy");

        Assert.DoesNotContain("Use this page to detail your site", html);
        Assert.Contains("POPIA", html);
    }
}

[Trait("Category", "Integration")]
public class RateLimitingTests
{
    private sealed class StrictFactory : TestApplicationFactory
    {
        protected override Dictionary<string, string?> Settings
        {
            get
            {
                var settings = base.Settings;
                settings["RateLimiting:PostPermitLimit"] = "2";
                return settings;
            }
        }
    }

    [Fact]
    public async Task PublicPosts_AreLimitedPerClient_ButTheWebhookIsNot()
    {
        using var factory = new StrictFactory();
        var client = factory.CreateClientFor();
        var token = await TestApplicationFactory.GetAntiforgeryTokenAsync(client, "/Home/Contact");
        var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await client.PostAsync("/Home/Contact", new FormUrlEncodedContent(form))).StatusCode);
        }

        var webhook = await client.PostAsync("/Payments/Notify",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["m_payment_id"] = "x" }));

        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[2]);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, webhook.StatusCode);
    }
}
