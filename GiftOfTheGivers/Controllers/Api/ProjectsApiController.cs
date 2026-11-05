using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Controllers.Api;

[ApiController]
[Route("api/v1/projects")]
[IgnoreAntiforgeryToken]
public class ProjectsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns relief projects with optional filtering by search query and project status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ReliefProjectApiDto>>> GetProjects(
        [FromQuery] string? search = null,
        [FromQuery] ProjectStatus? status = null)
    {
        var query = _context.ReliefProjects
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }
        else
        {
            query = query.Where(p => p.Status == ProjectStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) || p.Location.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        var list = await query
            .OrderByDescending(p => p.CreatedDate)
            .Select(p => new ReliefProjectApiDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Location = p.Location,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status.ToString(),
                FundsRequired = p.FundsRequired,
                OfflineFundsZar = p.OfflineFundsZar,
                FundsRaised = p.FundsRaised,
                ProgressPercentage = p.FundsRequired > 0 ? (double)Math.Min(100m, Math.Round((p.FundsRaised / p.FundsRequired) * 100m, 1)) : 0,
                ImageUrl = p.ImageUrl,
                CreatedDate = p.CreatedDate,
                UpdatesCount = p.ProjectUpdates.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Returns details for a single relief project by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReliefProjectApiDto>> GetProject(int id)
    {
        var project = await _context.ReliefProjects
            .AsNoTracking()
            .Include(p => p.ProjectUpdates)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
        {
            return NotFound(new { message = $"Relief project with ID {id} was not found." });
        }

        var dto = new ReliefProjectApiDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            Location = project.Location,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status.ToString(),
            FundsRequired = project.FundsRequired,
            OfflineFundsZar = project.OfflineFundsZar,
            FundsRaised = project.FundsRaised,
            ProgressPercentage = project.FundsRequired > 0 ? (double)Math.Min(100m, Math.Round((project.FundsRaised / project.FundsRequired) * 100m, 1)) : 0,
            ImageUrl = project.ImageUrl,
            CreatedDate = project.CreatedDate,
            UpdatesCount = project.ProjectUpdates.Count
        };

        return Ok(dto);
    }

    /// <summary>
    /// Returns all field updates posted for a given relief project.
    /// </summary>
    [HttpGet("{id:int}/updates")]
    public async Task<ActionResult<List<ProjectUpdateApiDto>>> GetProjectUpdates(int id)
    {
        var projectExists = await _context.ReliefProjects.AnyAsync(p => p.Id == id);
        if (!projectExists)
        {
            return NotFound(new { message = $"Relief project with ID {id} was not found." });
        }

        var updates = await _context.ProjectUpdates
            .AsNoTracking()
            .Include(u => u.PostedByUser)
            .Where(u => u.ReliefProjectId == id)
            .OrderByDescending(u => u.CreatedDate)
            .Select(u => new ProjectUpdateApiDto
            {
                Id = u.Id,
                ReliefProjectId = u.ReliefProjectId,
                Title = u.Title,
                Content = u.Content,
                ImageUrl = u.ImageUrl,
                CreatedDate = u.CreatedDate,
                AuthorName = u.PostedByUser != null ? u.PostedByUser.FullName : (!string.IsNullOrEmpty(u.CreatedBy) ? u.CreatedBy : "Field Officer")
            })
            .ToListAsync();

        return Ok(updates);
    }
}
