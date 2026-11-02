using GiftOfTheGivers.Data;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.ViewComponents;

/// <summary>
/// Impact figures computed from this system's own data. Replaces hard-coded marketing numbers
/// that could not be verified.
/// </summary>
public sealed class ImpactStatsViewComponent(ApplicationDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string style = "band")
    {
        var model = new ImpactStatsViewModel
        {
            ActiveProjects = await context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Active),
            CompletedProjects = await context.ReliefProjects.CountAsync(p => p.Status == ProjectStatus.Completed),
            ApprovedVolunteers = await context.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Approved),
            VerifiedDonations = await context.Donations.CountAsync(d => d.PaymentStatus == PaymentStatus.Verified),
            TotalRaisedZar = await context.ReliefProjects.SumAsync(p => (decimal?)p.FundsRaised) ?? 0m
        };

        return View(style == "cards" ? "Cards" : "Default", model);
    }
}
