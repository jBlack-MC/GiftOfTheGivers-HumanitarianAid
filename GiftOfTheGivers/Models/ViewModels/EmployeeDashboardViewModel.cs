namespace GiftOfTheGivers.Models.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public int ActiveProjects { get; set; }
        public int PendingVolunteers { get; set; }
        public int TotalDonationsThisMonth { get; set; }
        public decimal TotalAmountRaisedThisMonth { get; set; }
        public List<ReliefProject> RecentProjects { get; set; } = new List<ReliefProject>();
        public List<Volunteer> RecentVolunteerApplications { get; set; } = new List<Volunteer>();
    }
}
