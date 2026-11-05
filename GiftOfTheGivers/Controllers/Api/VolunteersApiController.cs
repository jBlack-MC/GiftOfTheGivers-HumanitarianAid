using GiftOfTheGivers.Data;
using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Controllers.Api;

[ApiController]
[Route("api/v1/volunteers")]
[IgnoreAntiforgeryToken]
public class VolunteersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VolunteersApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Submits a new volunteer application.
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<VolunteerStatusApiResponse>> Apply([FromBody] VolunteerApplicationApiRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.PrivacyConsent)
        {
            return BadRequest(new { message = "You must agree to the privacy notice to submit a volunteer application." });
        }

        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Full Name and Email address are required." });
        }

        if (request.DateOfBirth.HasValue &&
            (request.DateOfBirth.Value > DateTime.UtcNow.AddYears(-16) || request.DateOfBirth.Value < DateTime.UtcNow.AddYears(-110)))
        {
            return BadRequest(new { message = "Volunteers must be at least 16 years old." });
        }

        var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var volunteer = new Volunteer
        {
            UserId = User.Identity?.IsAuthenticated == true ? User.UserId() : null,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Email = request.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            DateOfBirth = request.DateOfBirth?.Date,
            Skills = request.Skills,
            Availability = request.Availability,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Status = VolunteerStatus.Pending,
            ApplicationDate = DateTime.UtcNow,
            PrivacyConsentAt = DateTime.UtcNow
        };

        _context.Volunteers.Add(volunteer);
        await _context.SaveChangesAsync();

        var response = new VolunteerStatusApiResponse
        {
            Id = volunteer.Id,
            FullName = $"{volunteer.FirstName} {volunteer.LastName}".Trim(),
            Email = volunteer.Email,
            Status = volunteer.Status.ToString(),
            ApplicationDate = volunteer.ApplicationDate,
            AssignedProjectsCount = 0
        };

        return CreatedAtAction(nameof(GetStatus), new { email = volunteer.Email }, response);
    }

    /// <summary>
    /// Checks volunteer status by email or for the currently authenticated user.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<VolunteerStatusApiResponse>> GetStatus([FromQuery] string? email = null)
    {
        Volunteer? volunteer = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.UserId();
            volunteer = await _context.Volunteers
                .AsNoTracking()
                .Include(v => v.Assignments)
                .FirstOrDefaultAsync(v => v.UserId == userId);
        }

        if (volunteer is null && !string.IsNullOrWhiteSpace(email))
        {
            volunteer = await _context.Volunteers
                .AsNoTracking()
                .Include(v => v.Assignments)
                .Where(v => v.Email == email.Trim())
                .OrderByDescending(v => v.ApplicationDate)
                .FirstOrDefaultAsync();
        }

        if (volunteer is null)
        {
            return NotFound(new { message = "No volunteer record found." });
        }

        var response = new VolunteerStatusApiResponse
        {
            Id = volunteer.Id,
            FullName = $"{volunteer.FirstName} {volunteer.LastName}".Trim(),
            Email = volunteer.Email,
            Status = volunteer.Status.ToString(),
            ApplicationDate = volunteer.ApplicationDate,
            AssignedProjectsCount = volunteer.Assignments.Count
        };

        return Ok(response);
    }
}
