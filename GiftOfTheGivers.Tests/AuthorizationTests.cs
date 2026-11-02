using System.Net;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GiftOfTheGivers.Tests;

/// <summary>Audit findings R1 (self-service staff role) and the role/ownership matrix.</summary>
[Trait("Category", "Integration")]
public class AuthorizationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public AuthorizationTests(TestApplicationFactory factory) => _factory = factory;

    public static TheoryData<string> EmployeePages =>
    [
        "/Employee/Dashboard",
        "/Employee/ReliefProjects",
        "/Employee/Volunteers",
        "/Employee/Donations",
        "/Employee/ExportDonations",
        "/Employee/Inquiries",
        "/Employee/AuditLog",
        "/Employee/StaffAccess",
        "/Employee/FundingReconciliation",
        "/Employee/CreateReliefProject"
    ];

    [Theory]
    [MemberData(nameof(EmployeePages))]
    public async Task EmployeePages_RejectAnonymousUsers(string url)
    {
        var response = await _factory.CreateClientFor().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(EmployeePages))]
    public async Task EmployeePages_ForbidDonors(string url)
    {
        var response = await _factory.CreateClientFor("donor-1", "Donor").GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(EmployeePages))]
    public async Task EmployeePages_AllowEmployees(string url)
    {
        var response = await _factory.CreateClientFor("employee-1", "Employee").GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DonorPortal_ForbidsEmployeeOnlyUsersButCertificatesAllowThem()
    {
        var donation = await _factory.AddDonationAsync(null, PaymentStatus.Verified, donorId: "someone-else");
        var employee = _factory.CreateClientFor("employee-2", "Employee");

        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/Donor/Dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await employee.GetAsync($"/Donor/TaxCertificate/{donation.Id}")).StatusCode);
    }

    [Fact]
    public async Task Registration_AlwaysCreatesDonor_EvenWhenEmployeeRoleIsPosted()
    {
        var client = _factory.CreateClientFor();
        var email = $"attacker-{Guid.NewGuid():N}@test.local";

        var response = await TestApplicationFactory.PostFormAsync(client,
            "/Identity/Account/Register", "/Identity/Account/Register", new Dictionary<string, string>
            {
                ["Input.FullName"] = "Would Be Staff",
                ["Input.Email"] = email,
                ["Input.Password"] = "Str0ng!Passw0rd",
                ["Input.ConfirmPassword"] = "Str0ng!Passw0rd",
                ["Input.Role"] = "Employee"
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var roles = await _factory.WithServicesAsync(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var user = await users.Users.SingleAsync(u => u.Email == email);
            return await users.GetRolesAsync(user);
        });
        Assert.Equal(["Donor"], roles);
    }

    [Fact]
    public async Task RegisterPage_NoLongerOffersAnEmployeeOption()
    {
        var html = await _factory.CreateClientFor().GetStringAsync("/Identity/Account/Register");

        Assert.DoesNotContain("roleEmployee", html);
        Assert.DoesNotContain("Input.Role", html);
    }

    [Fact]
    public async Task StaffAccess_GrantIsAuditedAndSelfRevokeIsRefused()
    {
        var (employeeId, targetEmail) = await _factory.WithServicesAsync(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var employee = new AppUser { UserName = "boss@test.local", Email = "boss@test.local", FullName = "Boss" };
            await users.CreateAsync(employee, "Str0ng!Passw0rd");
            await users.AddToRoleAsync(employee, "Employee");
            var target = new AppUser { UserName = "new-staff@test.local", Email = "new-staff@test.local", FullName = "New Staff" };
            await users.CreateAsync(target, "Str0ng!Passw0rd");
            return (employee.Id, target.Email!);
        });
        var client = _factory.CreateClientFor(employeeId, "Employee");

        var grant = await TestApplicationFactory.PostFormAsync(client, "/Employee/StaffAccess", "/Employee/GrantEmployee",
            new Dictionary<string, string> { ["email"] = targetEmail });
        var selfRevoke = await TestApplicationFactory.PostFormAsync(client, "/Employee/StaffAccess", "/Employee/RevokeEmployee",
            new Dictionary<string, string> { ["userId"] = employeeId });

        Assert.Equal(HttpStatusCode.Redirect, grant.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, selfRevoke.StatusCode);
        var (targetIsEmployee, selfIsEmployee, audited) = await _factory.WithServicesAsync(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var db = services.GetRequiredService<GiftOfTheGivers.Data.ApplicationDbContext>();
            return (
                await users.IsInRoleAsync((await users.FindByEmailAsync(targetEmail))!, "Employee"),
                await users.IsInRoleAsync((await users.FindByIdAsync(employeeId))!, "Employee"),
                await db.AuditLogs.AnyAsync(a => a.Action == "EmployeeGranted" && a.UserId == employeeId));
        });
        Assert.True(targetIsEmployee);
        Assert.True(selfIsEmployee);
        Assert.True(audited);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await _factory.CreateClientFor().GetAsync("/Home/Privacy");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task LivenessProbe_IsHealthy()
    {
        var response = await _factory.CreateClientFor().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
