using System.Net;
using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GiftOfTheGivers.Tests;

/// <summary>Logged-in donor journey: dashboard totals, donation history and details.</summary>
[Trait("Category", "Integration")]
public class DonorPortalTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public DonorPortalTests(TestApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_CountsOnlyVerifiedDonations()
    {
        var donorId = "donor-" + Guid.NewGuid().ToString("N")[..8];
        var project = await _factory.AddProjectAsync("Donor dashboard project");
        await _factory.AddDonationAsync(project.Id, PaymentStatus.Verified, 300m, donorId);
        await _factory.AddDonationAsync(project.Id, PaymentStatus.Verified, 200m, donorId);
        await _factory.AddDonationAsync(project.Id, PaymentStatus.Pending, 999m, donorId);

        var html = await _factory.CreateClientFor(donorId, "Donor").GetStringAsync("/Donor/Dashboard");

        Assert.Contains("Donor Dashboard", html);
        Assert.Contains("500", html);
        Assert.Contains("Donor dashboard project", html);
    }

    [Fact]
    public async Task MyDonations_And_Details_ShowOnlyTheDonorsOwnDonations()
    {
        var donorId = "donor-" + Guid.NewGuid().ToString("N")[..8];
        var mine = await _factory.AddDonationAsync(null, PaymentStatus.Verified, 123m, donorId);
        var someoneElses = await _factory.AddDonationAsync(null, PaymentStatus.Verified, 456m, "other-donor");
        var client = _factory.CreateClientFor(donorId, "Donor");

        var list = await client.GetStringAsync("/Donor/MyDonations");
        Assert.Contains("123", list);
        Assert.DoesNotContain("456", list);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Donor/DonationDetails/{mine.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Donor/DonationDetails/{someoneElses.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Donor/DonationDetails")).StatusCode);
    }

    [Fact]
    public async Task PublicPages_RenderForAnonymousVisitors()
    {
        var client = _factory.CreateClientFor();

        foreach (var url in new[] { "/", "/Home/About", "/Home/ReliefProjects", "/Home/Privacy", "/Home/VolunteerConfirmation", "/Home/ContactConfirmation" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        }
    }
}

/// <summary>
/// Start-up seeding against the in-memory provider: roles always exist, demo data is created
/// only when asked for, and running the seeder twice does not duplicate anything.
/// </summary>
[Trait("Category", "Integration")]
public class DbSeederTests
{
    private sealed class DemoDataFactory : TestApplicationFactory
    {
        protected override Dictionary<string, string?> Settings
        {
            get
            {
                var settings = base.Settings;
                settings["Seed:DemoData"] = "true";
                return settings;
            }
        }
    }

    [Fact]
    public async Task DemoSeed_CreatesAccountsProjectsVolunteersAndDonations_Idempotently()
    {
        using var factory = new DemoDataFactory();
        _ = factory.CreateClient(); // starts the host, which runs the seeder once

        var (projects, volunteers, donations) = await factory.WithDbAsync(async db =>
            (await db.ReliefProjects.CountAsync(), await db.Volunteers.CountAsync(), await db.Donations.CountAsync()));
        Assert.True(projects >= 3);
        Assert.True(volunteers > 0);
        Assert.True(donations > 0);

        var isEmployee = await factory.WithServicesAsync(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var employee = await users.FindByEmailAsync("employee@giftofthegivers.org");
            return employee is not null && await users.IsInRoleAsync(employee, "Employee");
        });
        Assert.True(isEmployee);

        // Second run (e.g. an app restart) must not add duplicates.
        await factory.WithServicesAsync(async services =>
        {
            await DbSeeder.SeedAsync(services, services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>());
            return true;
        });
        Assert.Equal(projects, await factory.WithDbAsync(db => db.ReliefProjects.CountAsync()));
        Assert.Equal(donations, await factory.WithDbAsync(db => db.Donations.CountAsync()));
    }
}
