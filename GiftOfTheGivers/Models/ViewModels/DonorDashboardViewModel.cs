namespace GiftOfTheGivers.Models.ViewModels
{
    public class DonorDashboardViewModel
    {
        /// <summary>Number of verified (paid) donations.</summary>
        public int TotalDonations { get; set; }

        /// <summary>Rand value of verified donations.</summary>
        public decimal TotalAmountDonated { get; set; }

        /// <summary>Donations still awaiting payment confirmation.</summary>
        public int PendingDonations { get; set; }

        public List<Donation> RecentDonations { get; set; } = new List<Donation>();
        public List<ReliefProject> FeaturedProjects { get; set; } = new List<ReliefProject>();
    }
}
