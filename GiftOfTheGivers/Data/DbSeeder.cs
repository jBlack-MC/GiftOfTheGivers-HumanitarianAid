using GiftOfTheGivers.Infrastructure;
using GiftOfTheGivers.Models;
using GiftOfTheGivers.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GiftOfTheGivers.Data
{
    /// <summary>
    /// Startup data tasks, in order:
    /// 1. Apply migrations when <c>Database:MigrateOnStartup</c> is true (default; production
    ///    deployments should apply the idempotent script from the pipeline and set it false).
    /// 2. Ensure the Donor and Employee roles exist (every environment).
    /// 3. Grant Employee to <c>StaffAccess:BootstrapEmployeeEmails</c> (every environment).
    /// 4. Seed demo accounts and sample data only in Development or when <c>Seed:DemoData</c> is true.
    ///
    /// Demo credentials (Development only — never enable Seed:DemoData on a public site):
    ///   Employee : employee@giftofthegivers.org  /  Employee#123
    ///   Donor    : donor@example.com             /  Donor#123
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IWebHostEnvironment env)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

            if (context.Database.IsRelational())
            {
                if (configuration.GetValue("Database:MigrateOnStartup", true))
                {
                    await context.Database.MigrateAsync();
                }
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            foreach (var role in new[] { ClaimsPrincipalExtensions.DonorRole, ClaimsPrincipalExtensions.EmployeeRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var staffOptions = services.GetRequiredService<IOptions<StaffAccessOptions>>().Value;
            foreach (var email in staffOptions.BootstrapEmployeeEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                var user = await userManager.FindByEmailAsync(email.Trim());
                if (user is null)
                {
                    logger.LogWarning("Bootstrap employee {Email} has no account yet; register it and restart.", email);
                }
                else if (!await userManager.IsInRoleAsync(user, ClaimsPrincipalExtensions.EmployeeRole))
                {
                    await userManager.AddToRoleAsync(user, ClaimsPrincipalExtensions.EmployeeRole);
                    logger.LogInformation("Granted Employee role to bootstrap account {Email}", email);
                }
            }

            if (!env.IsDevelopment() && !configuration.GetValue("Seed:DemoData", false))
            {
                return;
            }

            var employee = await EnsureUserAsync(userManager,
                "employee@giftofthegivers.org", "Employee#123", "Thandeka Mokoena", ClaimsPrincipalExtensions.EmployeeRole);
            var donor = await EnsureUserAsync(userManager,
                "donor@example.com", "Donor#123", "Sipho Ndlovu", ClaimsPrincipalExtensions.DonorRole);

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
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create demo user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
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

            // Offline funds represent money raised outside the platform (grants, cash, pledges).
            // FundsRaised starts equal to it and grows only when online payments are verified.
            static ReliefProject Project(string title, string description, string location, int monthsAgo,
                decimal required, decimal offline, string employeeId) => new()
            {
                Title = title,
                Description = description,
                Location = location,
                StartDate = DateTime.UtcNow.Date.AddMonths(-monthsAgo),
                Status = ProjectStatus.Active,
                FundsRequired = required,
                OfflineFundsZar = offline,
                FundsRaised = offline,
                CreatedByUserId = employeeId,
                CreatedDate = DateTime.UtcNow.AddMonths(-monthsAgo)
            };

            context.ReliefProjects.AddRange(
                Project("Flood Relief",
                    "Providing emergency supplies, shelter, and clean water to families affected by severe flooding in the region.",
                    "Johannesburg", 2, 1_000_000, 750_000, employeeId),
                Project("Community Food Bank",
                    "Distributing nutritious meals and food parcels to vulnerable communities facing food insecurity.",
                    "Cape Town", 4, 500_000, 300_000, employeeId),
                Project("Mobile Clinic Service",
                    "Providing free medical consultations, medications, and healthcare services to underserved areas.",
                    "Durban", 6, 1_000_000, 850_000, employeeId),
                Project("Fire Emergency Response",
                    "Urgent assistance for families displaced by wildfires, providing temporary shelter and essential supplies.",
                    "Knysna", 1, 500_000, 200_000, employeeId),
                Project("School Building Project",
                    "Constructing and renovating school facilities to provide better education infrastructure for rural communities.",
                    "Limpopo", 8, 2_000_000, 1_100_000, employeeId),
                Project("Drought Relief",
                    "Providing water tanks, boreholes, and livestock support to farming communities affected by severe drought.",
                    "Northern Cape", 5, 1_500_000, 870_000, employeeId));

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
                    Status = VolunteerStatus.Approved,
                    ApplicationDate = DateTime.UtcNow.AddDays(-20),
                    ApprovalDate = DateTime.UtcNow.AddDays(-14),
                    PrivacyConsentAt = DateTime.UtcNow.AddDays(-20)
                },
                new Volunteer
                {
                    FirstName = "Naledi",
                    LastName = "Khumalo",
                    Email = "naledi.k@example.com",
                    PhoneNumber = "0739876543",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    Skills = "Registered nurse, triage, community health",
                    Availability = "Weekdays",
                    Status = VolunteerStatus.Approved,
                    ApplicationDate = DateTime.UtcNow.AddDays(-40),
                    ApprovalDate = DateTime.UtcNow.AddDays(-33),
                    PrivacyConsentAt = DateTime.UtcNow.AddDays(-40)
                },
                new Volunteer
                {
                    FirstName = "Johan",
                    LastName = "Pretorius",
                    Email = "johan.p@example.com",
                    City = "Knysna",
                    Province = "Western Cape",
                    Skills = "Search and rescue, 4x4 recovery",
                    Availability = "Anytime",
                    Status = VolunteerStatus.Pending,
                    ApplicationDate = DateTime.UtcNow.AddDays(-3),
                    PrivacyConsentAt = DateTime.UtcNow.AddDays(-3)
                });

            await context.SaveChangesAsync();
        }

        private static async Task SeedDonationsAsync(ApplicationDbContext context, string donorUserId)
        {
            if (await context.Donations.AnyAsync())
            {
                return;
            }

            var firstProject = await context.ReliefProjects.OrderBy(p => p.Id).FirstOrDefaultAsync();

            // Two verified donations (counted in the project total) and one still pending
            // (not counted), so the dashboards and reconciliation show both states.
            var donations = new[]
            {
                new Donation
                {
                    DonorId = donorUserId, ReliefProjectId = firstProject?.Id, DonationType = "OneTime",
                    Currency = "ZAR", Amount = 500m, AmountZar = 500m, ExchangeRateToZar = 1m, PaymentMethod = "Card",
                    DonationDate = DateTime.UtcNow.AddDays(-10), TransactionReference = "SEED000000000001",
                    DonorName = "Sipho Ndlovu", DonorEmail = "donor@example.com",
                    PaymentStatus = PaymentStatus.Verified, PaymentVerifiedAt = DateTime.UtcNow.AddDays(-10),
                    TaxCertificateIssued = true, PrivacyConsentAt = DateTime.UtcNow.AddDays(-10)
                },
                new Donation
                {
                    DonorId = donorUserId, ReliefProjectId = firstProject?.Id, DonationType = "Recurring",
                    Currency = "ZAR", Amount = 150m, AmountZar = 150m, ExchangeRateToZar = 1m, PaymentMethod = "EFT",
                    DonationDate = DateTime.UtcNow.AddDays(-3), TransactionReference = "SEED000000000002",
                    DonorName = "Sipho Ndlovu", DonorEmail = "donor@example.com",
                    PaymentStatus = PaymentStatus.Pending, PrivacyConsentAt = DateTime.UtcNow.AddDays(-3)
                },
                new Donation
                {
                    DonorId = null, ReliefProjectId = firstProject?.Id, DonationType = "OneTime",
                    Currency = "USD", Amount = 75m, AmountZar = 1387.50m, ExchangeRateToZar = 18.50m, PaymentMethod = "Card",
                    DonationDate = DateTime.UtcNow.AddDays(-1), TransactionReference = "SEED000000000003",
                    DonorName = "Anonymous", IsAnonymous = true,
                    PaymentStatus = PaymentStatus.Verified, PaymentVerifiedAt = DateTime.UtcNow.AddDays(-1),
                    TaxCertificateIssued = true, PrivacyConsentAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            context.Donations.AddRange(donations);
            if (firstProject is not null)
            {
                firstProject.FundsRaised += donations
                    .Where(d => d.PaymentStatus == PaymentStatus.Verified)
                    .Sum(d => d.AmountZar);
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedAssignmentsAsync(ApplicationDbContext context)
        {
            if (await context.VolunteerAssignments.AnyAsync())
            {
                return;
            }

            var volunteers = await context.Volunteers
                .Where(v => v.Status == VolunteerStatus.Approved)
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
                        AssignedDate = DateTime.UtcNow.AddDays(-7)
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
