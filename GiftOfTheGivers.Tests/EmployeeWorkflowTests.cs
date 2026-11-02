using System.Net;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Tests;

/// <summary>
/// End-to-end employee workflows through the real MVC pipeline (routing, anti-forgery,
/// authorization, model binding, EF Core): project creation, posting updates, volunteer
/// approval and assignment, and inquiry handling. Each test checks the database afterwards and
/// the audit trail that staff actions must leave.
/// </summary>
[Trait("Category", "Integration")]
public class EmployeeWorkflowTests : IClassFixture<TestApplicationFactory>
{
    private const string EmployeeId = "employee-wf";
    private readonly TestApplicationFactory _factory;

    public EmployeeWorkflowTests(TestApplicationFactory factory) => _factory = factory;

    private HttpClient Employee() => _factory.CreateClientFor(EmployeeId, "Employee");

    private Task<bool> AuditedAsync(string action, int targetId) =>
        _factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == action && a.TargetId == targetId));

    [Fact]
    public async Task CreateReliefProject_SavesProject_AndAudits()
    {
        var title = "Drought relief " + Guid.NewGuid().ToString("N")[..6];

        var response = await TestApplicationFactory.PostFormAsync(Employee(), "/Employee/CreateReliefProject",
            "/Employee/CreateReliefProject", new Dictionary<string, string>
            {
                ["Title"] = title,
                ["Description"] = "Water tankers for drought-hit villages.",
                ["Location"] = "Northern Cape",
                ["Status"] = "Active",
                ["StartDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["FundsRequired"] = "250000",
                ["OfflineFundsZar"] = "10000"
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var project = await _factory.WithDbAsync(db => db.ReliefProjects.AsNoTracking().SingleAsync(p => p.Title == title));
        Assert.Equal(10_000m, project.FundsRaised);
        Assert.Equal(EmployeeId, project.CreatedByUserId);
        Assert.True(await AuditedAsync("ProjectCreated", project.Id));
    }

    [Fact]
    public async Task CreateReliefProject_EndBeforeStart_IsRejected()
    {
        var response = await TestApplicationFactory.PostFormAsync(Employee(), "/Employee/CreateReliefProject",
            "/Employee/CreateReliefProject", new Dictionary<string, string>
            {
                ["Title"] = "Bad dates",
                ["Description"] = "x",
                ["Location"] = "x",
                ["Status"] = "Active",
                ["StartDate"] = "2026-06-10",
                ["EndDate"] = "2026-06-01",
                ["FundsRequired"] = "1000",
                ["OfflineFundsZar"] = "0"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await _factory.WithDbAsync(db => db.ReliefProjects.AnyAsync(p => p.Title == "Bad dates")));
    }

    [Fact]
    public async Task PostProjectUpdate_ThenEditIt()
    {
        var project = await _factory.AddProjectAsync("Update target");
        var client = Employee();

        var created = await TestApplicationFactory.PostFormAsync(client,
            $"/Employee/CreateProjectUpdate?projectId={project.Id}", "/Employee/CreateProjectUpdate",
            new Dictionary<string, string>
            {
                ["ReliefProjectId"] = project.Id.ToString(),
                ["Title"] = "Day 3: 400 food parcels delivered",
                ["Content"] = "Teams reached the last two villages today."
            });

        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var update = await _factory.WithDbAsync(db =>
            db.ProjectUpdates.AsNoTracking().SingleAsync(u => u.ReliefProjectId == project.Id));
        Assert.Equal(EmployeeId, update.PostedByUserId);
        Assert.True(await AuditedAsync("ProjectUpdatePosted", update.Id));

        var edited = await TestApplicationFactory.PostFormAsync(client,
            $"/Employee/EditProjectUpdate/{update.Id}", "/Employee/EditProjectUpdate",
            new Dictionary<string, string>
            {
                ["Id"] = update.Id.ToString(),
                ["ReliefProjectId"] = project.Id.ToString(),
                ["Title"] = "Day 3: 450 food parcels delivered",
                ["Content"] = "Corrected count after the evening tally."
            });

        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        var saved = await _factory.WithDbAsync(db => db.ProjectUpdates.AsNoTracking().SingleAsync(u => u.Id == update.Id));
        Assert.Equal("Day 3: 450 food parcels delivered", saved.Title);
        Assert.NotNull(saved.LastModifiedDate);
    }

    [Fact]
    public async Task PostProjectUpdate_MissingTitle_RedisplaysForm()
    {
        var project = await _factory.AddProjectAsync("Validation target");

        var response = await TestApplicationFactory.PostFormAsync(Employee(),
            $"/Employee/CreateProjectUpdate?projectId={project.Id}", "/Employee/CreateProjectUpdate",
            new Dictionary<string, string>
            {
                ["ReliefProjectId"] = project.Id.ToString(),
                ["Title"] = "",
                ["Content"] = "No title"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await _factory.WithDbAsync(db => db.ProjectUpdates.AnyAsync(u => u.ReliefProjectId == project.Id)));
    }

    [Fact]
    public async Task UnknownProjectOrUpdate_Returns404()
    {
        var client = Employee();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Employee/CreateProjectUpdate?projectId=999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Employee/EditProjectUpdate/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Employee/VolunteerDetails/999999")).StatusCode);
    }

    [Fact]
    public async Task Volunteer_ApprovedThenAssignedThenUnassigned()
    {
        var project = await _factory.AddProjectAsync("Volunteer project");
        var volunteer = await _factory.WithDbAsync(async db =>
        {
            var v = new Volunteer
            {
                FirstName = "Ayanda",
                LastName = "Khumalo",
                Email = "ayanda@test.local",
                Skills = "First aid",
                Availability = "Weekends"
            };
            db.Volunteers.Add(v);
            await db.SaveChangesAsync();
            return v;
        });
        var client = Employee();
        var detailsUrl = $"/Employee/VolunteerDetails/{volunteer.Id}";

        // A pending volunteer cannot be assigned yet.
        await TestApplicationFactory.PostFormAsync(client, detailsUrl, "/Employee/AssignVolunteer",
            new Dictionary<string, string> { ["volunteerId"] = volunteer.Id.ToString(), ["reliefProjectId"] = project.Id.ToString() });
        Assert.False(await _factory.WithDbAsync(db => db.VolunteerAssignments.AnyAsync(a => a.VolunteerId == volunteer.Id)));

        await TestApplicationFactory.PostFormAsync(client, detailsUrl, "/Employee/ApproveVolunteer",
            new Dictionary<string, string> { ["id"] = volunteer.Id.ToString() });
        Assert.Equal(VolunteerStatus.Approved,
            (await _factory.WithDbAsync(db => db.Volunteers.AsNoTracking().SingleAsync(v => v.Id == volunteer.Id))).Status);
        Assert.True(await AuditedAsync("VolunteerApproved", volunteer.Id));

        await TestApplicationFactory.PostFormAsync(client, detailsUrl, "/Employee/AssignVolunteer",
            new Dictionary<string, string> { ["volunteerId"] = volunteer.Id.ToString(), ["reliefProjectId"] = project.Id.ToString() });
        var assignment = await _factory.WithDbAsync(db =>
            db.VolunteerAssignments.AsNoTracking().SingleAsync(a => a.VolunteerId == volunteer.Id));
        Assert.Equal(project.Id, assignment.ReliefProjectId);
        Assert.Contains("Volunteer project", await client.GetStringAsync(detailsUrl));

        await TestApplicationFactory.PostFormAsync(client, detailsUrl, "/Employee/UnassignVolunteer",
            new Dictionary<string, string> { ["assignmentId"] = assignment.Id.ToString() });
        Assert.False(await _factory.WithDbAsync(db => db.VolunteerAssignments.AnyAsync(a => a.Id == assignment.Id)));

        await TestApplicationFactory.PostFormAsync(client, detailsUrl, "/Employee/RejectVolunteer",
            new Dictionary<string, string> { ["id"] = volunteer.Id.ToString() });
        Assert.Equal(VolunteerStatus.Rejected,
            (await _factory.WithDbAsync(db => db.Volunteers.AsNoTracking().SingleAsync(v => v.Id == volunteer.Id))).Status);
    }

    [Fact]
    public async Task Inquiry_MarkedHandled()
    {
        var inquiry = await _factory.WithDbAsync(async db =>
        {
            var i = new Inquiry { Name = "Pieter", Email = "pieter@test.local", Subject = "Drop-off point", Message = "Where can I drop blankets?" };
            db.Inquiries.Add(i);
            await db.SaveChangesAsync();
            return i;
        });

        var response = await TestApplicationFactory.PostFormAsync(Employee(), "/Employee/Inquiries",
            "/Employee/MarkInquiryHandled", new Dictionary<string, string> { ["id"] = inquiry.Id.ToString() });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True((await _factory.WithDbAsync(db => db.Inquiries.AsNoTracking().SingleAsync(i => i.Id == inquiry.Id))).Handled);
        Assert.True(await AuditedAsync("InquiryHandled", inquiry.Id));
    }

    [Fact]
    public async Task AcceptFundingAsOffline_MovesUnexplainedTotalIntoOfflineFunds()
    {
        var project = await _factory.AddProjectAsync("Reconcile me", offline: 100m);
        await _factory.AddDonationAsync(project.Id, PaymentStatus.Verified, 50m);
        await _factory.WithDbAsync(async db =>
        {
            var p = await db.ReliefProjects.SingleAsync(x => x.Id == project.Id);
            p.FundsRaised = 400m; // 100 offline + 50 verified + 250 unexplained (historical cash)
            await db.SaveChangesAsync();
        });

        await TestApplicationFactory.PostFormAsync(Employee(), "/Employee/FundingReconciliation",
            "/Employee/AcceptFundingAsOffline", new Dictionary<string, string> { ["id"] = project.Id.ToString() });

        var saved = await _factory.WithDbAsync(db => db.ReliefProjects.AsNoTracking().SingleAsync(p => p.Id == project.Id));
        Assert.Equal(350m, saved.OfflineFundsZar);
        Assert.Equal(400m, saved.FundsRaised);
    }
}
