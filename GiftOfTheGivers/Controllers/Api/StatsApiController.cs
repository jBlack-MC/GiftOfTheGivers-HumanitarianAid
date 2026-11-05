using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using GiftOfTheGivers.Services;

namespace GiftOfTheGivers.Controllers.Api;

[ApiController]
[Route("api/v1/stats")]
[IgnoreAntiforgeryToken]
public class StatsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _caching;
    private const string CacheKey = "api-impact-stats";

    public StatsApiController(
        ApplicationDbContext context,
        IMemoryCache cache,
        IOptions<CachingOptions> caching)
    {
        _context = context;
        _cache = cache;
        _caching = caching.Value;
    }

    /// <summary>
    /// Returns live impact metrics computed from database records.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ImpactStatsApiResponse>> GetStats()
    {
        var seconds = _caching.PublicPageSeconds;
        var stats = seconds > 0
            ? await _cache.GetOrCreateAsync(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds);
                return LoadStatsAsync();
            })
            : await LoadStatsAsync();

        return Ok(stats);
    }

    private async Task<ImpactStatsApiResponse> LoadStatsAsync() => new()
    {
        ActiveProjects = await _context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Active),
        CompletedProjects = await _context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Completed),
        ApprovedVolunteers = await _context.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Approved),
        VerifiedDonations = await _context.Donations.CountAsync(d => d.PaymentStatus == PaymentStatus.Verified),
        TotalRaisedZar = await _context.ReliefProjects.SumAsync(p => (decimal?)p.FundsRaised) ?? 0m
    };
}
