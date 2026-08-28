using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Data
{
    /// <summary>
    /// Seeds roles, two demo accounts and a small set of sample data so the
    /// prototype has something to show on first run.
    ///
    /// Demo credentials (Part 1 prototype only):
    ///   Employee : employee@giftofthegivers.org  /  Employee#123
    ///   Donor    : donor@example.com             /  Donor#123
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser> userManager)
        {
            foreach (var role in new[] { "Donor", "Employee" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var employee = await EnsureUserAsync(userManager,
                "employee@giftofthegivers.org", "Employee#123", "Thandeka Mokoena", "Employee");
            var donor = await EnsureUserAsync(userManager,
                "donor@example.com", "Donor#123", "Sipho Ndlovu", "Donor");

            await SeedReliefProjectsAsync(context, employee.Id);
            await SeedVolunteersAsync(context, donor.Id);
            await SeedDonationsAsync(context, donor.Id);
            await SeedAssignmentsAsync(context);
        }

        private static async Task<AppUser> EnsureUserAsync(
            UserManager<AppUser> userManager, string email, string password, string fullName, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    DateRegistered = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, password);
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }

        private static async Task SeedReliefProjectsAsync(ApplicationDbContext context, string employeeId)
        {
            if (await context.ReliefProjects.AnyAsync())
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
                    FundsRaised = 750_000,
                    CreatedByUserId = employeeId
                },
                new ReliefProject
                {
                    Title = "Community Food Bank",
                    Description = "Distributing nutritious meals and food parcels to vulnerable communities facing food insecurity.",
                    Location = "Cape Town",
                    StartDate = DateTime.Now.AddMonths(-4),
                    Status = "Active",
                    FundsRequired = 500_000,
                    FundsRaised = 300_000,
                    CreatedByUserId = employeeId
                },
                new ReliefProject
                {
                    Title = "Mobile Clinic Service",
                    Description = "Providing free medical consultations, medications, and healthcare services to underserved areas.",
                    Location = "Durban",
                    StartDate = DateTime.Now.AddMonths(-6),
                    Status = "Active",
                    FundsRequired = 1_000_000,
                    FundsRaised = 850_000,
                    CreatedByUserId = employeeId
                },
                new ReliefProject
                {
                    Title = "Fire Emergency Response",
                    Description = "Urgent assistance for families displaced by wildfires, providing temporary shelter and essential supplies.",
                    Location = "Knysna",
                    StartDate = DateTime.Now.AddMonths(-1),
                    Status = "Active",
                    FundsRequired = 500_000,
                    FundsRaised = 200_000,
                    CreatedByUserId = employeeId
                },
                new ReliefProject
                {
                    Title = "School Building Project",
                    Description = "Constructing and renovating school facilities to provide better education infrastructure for rural communities.",
                    Location = "Limpopo",
                    StartDate = DateTime.Now.AddMonths(-8),
                    Status = "Active",
                    FundsRequired = 2_000_000,
                    FundsRaised = 1_100_000,
                    CreatedByUserId = employeeId
                },
                new ReliefProject
                {
                    Title = "Drought Relief",
                    Description = "Providing water tanks, boreholes, and livestock support to farming communities affected by severe drought.",
                    Location = "Northern Cape",
                    StartDate = DateTime.Now.AddMonths(-5),
                    Status = "Active",
                    FundsRequired = 1_500_000,
                    FundsRaised = 870_000,
                    CreatedByUserId = employeeId
                }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedVolunteersAsync(ApplicationDbContext context, string donorUserId)
        {
            if (await context.Volunteers.AnyAsync())
            {
                return;
            }

            context.Volunteers.AddRange(
                new Volunteer
                {
                    UserId = donorUserId,
                    FirstName = "Sipho",
                    LastName = "Ndlovu",
                    Email = "donor@example.com",
                    PhoneNumber = "0821234567",
                    City = "Cape Town",
                    Province = "Western Cape",
                    DateOfBirth = new DateTime(1994, 5, 12),
                    Skills = "Logistics, driving (Code 10), first aid",
                    Availability = "Weekends",
                    Status = "Approved",
                    ApplicationDate = DateTime.Now.AddDays(-20),
                    ApprovalDate = DateTime.Now.AddDays(-14)
                },
                new Volunteer
                {
                    FirstName = "Naledi",
                    LastName = "Khumalo",
                    Email = "naledi.k@example.com",
                    PhoneNumber = "0739876543",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    DateOfBirth = new DateTime(1990, 11, 3),
                    Skills = "Registered nurse, triage, community health",
                    Availability = "Weekdays",
                    Status = "Active",
                    ApplicationDate = DateTime.Now.AddDays(-40),
                    ApprovalDate = DateTime.Now.AddDays(-33)
                },
                new Volunteer
                {
                    FirstName = "Johan",
                    LastName = "Pretorius",
                    Email = "johan.p@example.com",
                    PhoneNumber = "0824567890",
                    City = "Knysna",
                    Province = "Western Cape",
                    DateOfBirth = new DateTime(1988, 2, 27),
                    Skills = "Search and rescue, 4x4 recovery",
                    Availability = "Anytime",
                    Status = "Pending",
                    ApplicationDate = DateTime.Now.AddDays(-3)
                }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedDonationsAsync(ApplicationDbContext context, string donorUserId)
        {
            if (await context.Donations.AnyAsync())
            {
                return;
            }

            var firstProjectId = await context.ReliefProjects
                .OrderBy(p => p.Id).Select(p => (int?)p.Id).FirstOrDefaultAsync();

            context.Donations.AddRange(
                new Donation
                {
                    DonorId = donorUserId,
                    ReliefProjectId = firstProjectId,
                    DonationType = "OneTime",
                    Currency = "ZAR",
                    Amount = 500m,
                    PaymentMethod = "Card",
                    DonationDate = DateTime.Now.AddDays(-10),
                    TransactionReference = "SEED000000000001",
                    IsAnonymous = false,
                    TaxCertificateIssued = true
                },
                new Donation
                {
                    DonorId = donorUserId,
                    ReliefProjectId = firstProjectId,
                    DonationType = "Recurring",
                    Currency = "ZAR",
                    Amount = 150m,
                    PaymentMethod = "EFT",
                    DonationDate = DateTime.Now.AddDays(-3),
                    TransactionReference = "SEED000000000002",
                    IsAnonymous = false
                },
                new Donation
                {
                    DonorId = null,
                    ReliefProjectId = firstProjectId,
                    DonationType = "OneTime",
                    Currency = "USD",
                    Amount = 75m,
                    PaymentMethod = "Card",
                    DonationDate = DateTime.Now.AddDays(-1),
                    TransactionReference = "SEED000000000003",
                    IsAnonymous = true
                }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedAssignmentsAsync(ApplicationDbContext context)
        {
            if (await context.VolunteerAssignments.AnyAsync())
            {
                return;
            }

            var volunteers = await context.Volunteers
                .Where(v => v.Status == "Approved" || v.Status == "Active")
                .OrderBy(v => v.Id).ToListAsync();
            var projects = await context.ReliefProjects
                .OrderBy(p => p.Id).Take(2).ToListAsync();

            if (volunteers.Count == 0 || projects.Count == 0)
            {
                return;
            }

            foreach (var volunteer in volunteers)
            {
                foreach (var project in projects)
                {
                    context.VolunteerAssignments.Add(new VolunteerAssignment
                    {
                        VolunteerId = volunteer.Id,
                        ReliefProjectId = project.Id,
                        AssignedDate = DateTime.Now.AddDays(-7)
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
