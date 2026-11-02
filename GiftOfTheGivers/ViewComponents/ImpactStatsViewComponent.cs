using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.ViewComponents;

/// <summary>
/// Impact figures computed from this system's own data. Replaces hard-coded marketing numbers
/// that could not be verified. The five aggregate queries are cached for
/// <see cref="CachingOptions.PublicPageSeconds"/> because they run on every home page view.
/// </summary>
public sealed class ImpactStatsViewComponent(
    ApplicationDbContext context,
    IMemoryCache cache,
    IOptions<CachingOptions> caching) : ViewComponent
{
    internal const string CacheKey = "impact-stats";

    public async Task<IViewComponentResult> InvokeAsync(string style = "band")
    {
        var seconds = caching.Value.PublicPageSeconds;
        var model = seconds > 0
            ? await cache.GetOrCreateAsync(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds);
                return LoadAsync();
            })
            : await LoadAsync();

        return View(style == "cards" ? "Cards" : "Default", model!);
    }

    private async Task<ImpactStatsViewModel> LoadAsync() => new()
    {
        ActiveProjects = await context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Active),
        CompletedProjects = await context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Completed),
        ApprovedVolunteers = await context.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Approved),
        VerifiedDonations = await context.Donations.CountAsync(d => d.PaymentStatus == PaymentStatus.Verified),
        TotalRaisedZar = await context.ReliefProjects.SumAsync(p => (decimal?)p.FundsRaised) ?? 0m
    };
}
