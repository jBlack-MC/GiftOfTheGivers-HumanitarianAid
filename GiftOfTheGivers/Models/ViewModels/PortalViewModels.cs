namespace GiftOfTheGivers.Models.ViewModels;

/// <summary>One page of a server-side paged list.</summary>
public sealed class PagedList<T>
{
    public const int DefaultPageSize = 25;

    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>Clamps a requested page number to a valid 1-based value.</summary>
    public static int NormalisePage(int? page) => page is null or < 1 ? 1 : page.Value;
}

public sealed class DonationsPageViewModel
{
    public required PagedList<Donation> Donations { get; init; }
    public PaymentStatus? StatusFilter { get; init; }
    public int VerifiedCount { get; init; }
    public decimal VerifiedTotalZar { get; init; }
    public int PendingCount { get; init; }
}

public sealed class VolunteersPageViewModel
{
    public required PagedList<Volunteer> Volunteers { get; init; }
    public VolunteerStatus? StatusFilter { get; init; }
    public int AllCount { get; init; }
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }
}

public sealed class AuditLogEntryViewModel
{
    public required AuditLog Entry { get; init; }
    public string? UserEmail { get; init; }
}

public sealed class VolunteerDetailsViewModel
{
    public required Volunteer Volunteer { get; init; }
    public required IReadOnlyList<VolunteerAssignment> Assignments { get; init; }
    public required IReadOnlyList<ReliefProject> AssignableProjects { get; init; }
}

public sealed class FundingReconciliationRow
{
    public required ReliefProject Project { get; init; }
    public decimal VerifiedOnlineZar { get; init; }
    public decimal PendingOnlineZar { get; init; }
    public decimal ExpectedFundsRaised => Project.OfflineFundsZar + VerifiedOnlineZar;
    public decimal Difference => Project.FundsRaised - ExpectedFundsRaised;
    public bool InBalance => Difference == 0m;
}

public sealed class StaffAccessViewModel
{
    public required IReadOnlyList<AppUser> Employees { get; init; }
    public string? CurrentUserId { get; init; }
}

public sealed class DonationReceiptViewModel
{
    public required Donation Donation { get; init; }
    public bool PaymentsEnabled { get; init; }
    public bool CanViewCertificate { get; init; }
}

public sealed class ImpactStatsViewModel
{
    public int ActiveProjects { get; init; }
    public int CompletedProjects { get; init; }
    public int ApprovedVolunteers { get; init; }
    public int VerifiedDonations { get; init; }
    public decimal TotalRaisedZar { get; init; }
}
