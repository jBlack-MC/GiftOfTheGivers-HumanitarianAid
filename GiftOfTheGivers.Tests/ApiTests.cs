using System.Net;
using System.Net.Http.Json;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Tests;

[Trait("Category", "Integration")]
public class ApiTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ApiTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_ReturnsActiveProjects_AsJson()
    {
        await _factory.AddProjectAsync("API Relief Mission", ProjectStatus.Active);
        var client = _factory.CreateClientFor();
        var response = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ReliefProjectApiDto>>();
        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
        Assert.Contains(projects, p => p.Title == "API Relief Mission");
    }

    [Fact]
    public async Task GetProject_ById_ReturnsDetails()
    {
        var project = await _factory.AddProjectAsync("Specific Mission", ProjectStatus.Active);
        var client = _factory.CreateClientFor();
        var response = await client.GetAsync($"/api/v1/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ReliefProjectApiDto>();
        Assert.NotNull(dto);
        Assert.Equal(project.Id, dto.Id);
        Assert.Equal("Specific Mission", dto.Title);
    }

    [Fact]
    public async Task GetProject_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClientFor();
        var response = await client.GetAsync("/api/v1/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonation_Api_ReturnsCreatedWithCheckoutUrl()
    {
        var project = await _factory.AddProjectAsync("Donation Project", ProjectStatus.Active);
        var client = _factory.CreateClientFor();

        var request = new CreateDonationApiRequest
        {
            ReliefProjectId = project.Id,
            Amount = 150m,
            Currency = "ZAR",
            DonationType = "OneTime",
            PaymentMethod = "Card",
            DonorName = "Mobile Donor",
            DonorEmail = "mobiledonor@test.local",
            IsAnonymous = false
        };

        var response = await client.PostAsJsonAsync("/api/v1/donations", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DonationApiResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.TransactionReference);
        Assert.Equal(150m, result.Amount);
        Assert.Equal("Pending", result.PaymentStatus);
    }

    [Fact]
    public async Task GetStats_Api_ReturnsLiveMetrics()
    {
        var client = _factory.CreateClientFor();
        var response = await client.GetAsync("/api/v1/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<ImpactStatsApiResponse>();
        Assert.NotNull(stats);
        Assert.True(stats.ActiveProjects >= 0);
    }

    [Fact]
    public async Task ApplyVolunteer_Api_ValidApplication_SavesAndReturnsCreated()
    {
        var client = _factory.CreateClientFor();
        var email = $"api-vol-{Guid.NewGuid():N}@test.local";

        var request = new VolunteerApplicationApiRequest
        {
            FullName = "Mobile Volunteer",
            Email = email,
            PhoneNumber = "0829998877",
            Skills = "Logistics, Medical",
            Availability = "Weekends",
            EmergencyContactName = "Jane Volunteer",
            EmergencyContactPhone = "0821112233",
            PrivacyConsent = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/volunteers/apply", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var volunteer = await _factory.WithDbAsync(db => db.Volunteers.AsNoTracking().SingleOrDefaultAsync(v => v.Email == email));
        Assert.NotNull(volunteer);
        Assert.Equal("Mobile", volunteer.FirstName);
        Assert.Equal("Volunteer", volunteer.LastName);
    }

    [Fact]
    public async Task ApplyVolunteer_WithoutConsent_ReturnsBadRequest()
    {
        var client = _factory.CreateClientFor();
        var request = new VolunteerApplicationApiRequest
        {
            FullName = "No Consent Mobile",
            Email = "noconsent@test.local",
            PrivacyConsent = false
        };

        var response = await client.PostAsJsonAsync("/api/v1/volunteers/apply", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInquiry_Api_SavesInquiryAndReturnsOk()
    {
        var client = _factory.CreateClientFor();
        var request = new InquiryApiRequest
        {
            Name = "Mobile User",
            Email = "mobileuser@test.local",
            Subject = "App Support Inquiry",
            Message = "How do I volunteer for emergency floods?",
            PrivacyConsent = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/inquiries", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Auth_RegisterAndLoginFlow_Succeeds()
    {
        var client = _factory.CreateClientFor();
        var email = $"api-auth-{Guid.NewGuid():N}@test.local";
        var registerRequest = new RegisterApiRequest
        {
            FullName = "API Test User",
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);
        var regResult = await regResponse.Content.ReadFromJsonAsync<AuthApiResponse>();
        Assert.NotNull(regResult);
        Assert.True(regResult.Success);
        Assert.Equal(email, regResult.Email);

        var loginRequest = new LoginApiRequest
        {
            Email = email,
            Password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthApiResponse>();
        Assert.NotNull(loginResult);
        Assert.True(loginResult.Success);
    }
}
