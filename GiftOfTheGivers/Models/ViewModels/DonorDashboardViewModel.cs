namespace GiftOfTheGivers.Models.ViewModels
{
    public class DonorDashboardViewModel
    {
        public int TotalDonations { get; set; }
        public decimal TotalAmountDonated { get; set; }
        public List<Donation> RecentDonations { get; set; } = new List<Donation>();
        public List<ReliefProject> FeaturedProjects { get; set; } = new List<ReliefProject>();
    }
}
