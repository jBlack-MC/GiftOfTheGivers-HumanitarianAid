namespace GiftOfTheGivers.Models.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public int ActiveProjects { get; set; }
        public int PendingVolunteers { get; set; }

        /// <summary>Verified donations since the first day of the current month (UTC).</summary>
        public int TotalDonationsThisMonth { get; set; }

        /// <summary>Rand value of those verified donations.</summary>
        public decimal TotalAmountRaisedThisMonth { get; set; }

        public int PendingPayments { get; set; }
        public int OpenInquiries { get; set; }
        public int ProjectsOutOfBalance { get; set; }

        public List<ReliefProject> RecentProjects { get; set; } = new List<ReliefProject>();
        public List<Volunteer> RecentVolunteerApplications { get; set; } = new List<Volunteer>();
    }
}
