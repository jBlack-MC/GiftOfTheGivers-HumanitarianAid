using GiftOfTheGivers.Models;

namespace GiftOfTheGivers.Data
{
    public static class DbSeeder
    {
        public static async Task SeedReliefProjectsAsync(ApplicationDbContext context)
        {
            if (context.ReliefProjects.Any())
            {
                return;
            }

            context.ReliefProjects.AddRange(
                new ReliefProject
                {
                    Title = "Flood Relief",
                    Description = "Providing emergency supplies, shelter, and clean water to families affected by severe flooding in the region.",
                    Location = "Johannesburg",
                    StartDate = DateTime.Now.AddMonths(-2),
                    Status = "Active",
                    FundsRequired = 1_000_000,
                    FundsRaised = 750_000
                },
                new ReliefProject
                {
                    Title = "Community Food Bank",
                    Description = "Distributing nutritious meals and food parcels to vulnerable communities facing food insecurity.",
                    Location = "Cape Town",
                    StartDate = DateTime.Now.AddMonths(-4),
                    Status = "Active",
                    FundsRequired = 500_000,
                    FundsRaised = 300_000
                },
                new ReliefProject
                {
                    Title = "Mobile Clinic Service",
                    Description = "Providing free medical consultations, medications, and healthcare services to underserved areas.",
                    Location = "Durban",
                    StartDate = DateTime.Now.AddMonths(-6),
                    Status = "Active",
                    FundsRequired = 1_000_000,
                    FundsRaised = 850_000
                },
                new ReliefProject
                {
                    Title = "Fire Emergency Response",
                    Description = "Urgent assistance for families displaced by wildfires, providing temporary shelter and essential supplies.",
                    Location = "Knysna",
                    StartDate = DateTime.Now.AddMonths(-1),
                    Status = "Active",
                    FundsRequired = 500_000,
                    FundsRaised = 200_000
                },
                new ReliefProject
                {
                    Title = "School Building Project",
                    Description = "Constructing and renovating school facilities to provide better education infrastructure for rural communities.",
                    Location = "Limpopo",
                    StartDate = DateTime.Now.AddMonths(-8),
                    Status = "Active",
                    FundsRequired = 2_000_000,
                    FundsRaised = 1_100_000
                },
                new ReliefProject
                {
                    Title = "Drought Relief",
                    Description = "Providing water tanks, boreholes, and livestock support to farming communities affected by severe drought.",
                    Location = "Northern Cape",
                    StartDate = DateTime.Now.AddMonths(-5),
                    Status = "Active",
                    FundsRequired = 1_500_000,
                    FundsRaised = 870_000
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
