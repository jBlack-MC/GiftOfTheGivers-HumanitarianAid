using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<AppUser>(options)
    {
        public DbSet<ReliefProject> ReliefProjects { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Volunteer> Volunteers { get; set; }
        public DbSet<ProjectUpdate> ProjectUpdates { get; set; }
        public DbSet<VolunteerAssignment> VolunteerAssignments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---- decimal precision for currency fields -------------------
            builder.Entity<ReliefProject>().Property(p => p.FundsRequired).HasPrecision(18, 2);
            builder.Entity<ReliefProject>().Property(p => p.FundsRaised).HasPrecision(18, 2);
            builder.Entity<Donation>().Property(d => d.Amount).HasPrecision(18, 2);
            builder.Entity<ReliefProject>().Property(p => p.Status).HasConversion<string>();
            builder.Entity<Volunteer>().Property(v => v.Status).HasConversion<string>();

            // ---- check constraints (data integrity, Section 2.1/2.3) -----
            builder.Entity<Donation>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Donation_Currency", "[Currency] IN ('ZAR','USD','EUR')");
                t.HasCheckConstraint("CK_Donation_Type", "[DonationType] IN ('OneTime','Recurring')");
                t.HasCheckConstraint("CK_Donation_Amount", "[Amount] > 0");
            });
            builder.Entity<ReliefProject>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_ReliefProject_Status",
                    "[Status] IN ('Active','Completed','Suspended')");
                t.HasCheckConstraint("CK_ReliefProject_FundsRequired_NonNegative",
                    "[FundsRequired] >= 0");
                t.HasCheckConstraint("CK_ReliefProject_Dates",
                    "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            });
            builder.Entity<Volunteer>().ToTable(t =>
                t.HasCheckConstraint("CK_Volunteer_Status",
                    "[Status] IN ('Pending','Approved','Rejected')"));

            // ---- indexes on the columns used in joins / lookups ---------
            builder.Entity<Donation>().HasIndex(d => d.DonorId);
            builder.Entity<Donation>().HasIndex(d => d.ReliefProjectId);
            builder.Entity<Donation>().HasIndex(d => d.TransactionReference).IsUnique();
            builder.Entity<Donation>().HasIndex(d => d.IdempotencyKey).IsUnique();
            builder.Entity<Donation>().HasIndex(d => new { d.DonorId, d.DonationDate });
            builder.Entity<Volunteer>().HasIndex(v => v.UserId);
            builder.Entity<Volunteer>().HasIndex(v => v.Email);
            builder.Entity<Volunteer>().HasIndex(v => new { v.Status, v.ApplicationDate });
            builder.Entity<ProjectUpdate>().HasIndex(p => p.ReliefProjectId);
            builder.Entity<ProjectUpdate>().HasIndex(p => p.PostedByUserId);
            builder.Entity<ReliefProject>().HasIndex(p => p.CreatedByUserId);
            builder.Entity<AuditLog>().HasIndex(a => new { a.TargetEntity, a.TargetId });
            builder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
            builder.Entity<VolunteerAssignment>().HasIndex(a => a.VolunteerId);
            builder.Entity<VolunteerAssignment>().HasIndex(a => a.ReliefProjectId);
            builder.Entity<VolunteerAssignment>()
                .HasIndex(a => new { a.VolunteerId, a.ReliefProjectId })
                .IsUnique(); // a volunteer can only be assigned to a project once

            // ---- delete behaviour: keep SQL Server off multiple cascade paths
            builder.Entity<Donation>()
                .HasOne(d => d.Donor).WithMany()
                .HasForeignKey(d => d.DonorId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Donation>()
                .HasOne(d => d.ReliefProject).WithMany(p => p.Donations)
                .HasForeignKey(d => d.ReliefProjectId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Volunteer>()
                .HasOne(v => v.User).WithMany()
                .HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ReliefProject>()
                .HasOne(p => p.CreatedByUser).WithMany()
                .HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectUpdate>()
                .HasOne(u => u.ReliefProject).WithMany(p => p.ProjectUpdates)
                .HasForeignKey(u => u.ReliefProjectId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ProjectUpdate>()
                .HasOne(u => u.PostedByUser).WithMany()
                .HasForeignKey(u => u.PostedByUserId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VolunteerAssignment>()
                .HasOne(a => a.Volunteer).WithMany(v => v.Assignments)
                .HasForeignKey(a => a.VolunteerId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<VolunteerAssignment>()
                .HasOne(a => a.ReliefProject).WithMany(p => p.VolunteerAssignments)
                .HasForeignKey(a => a.ReliefProjectId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
