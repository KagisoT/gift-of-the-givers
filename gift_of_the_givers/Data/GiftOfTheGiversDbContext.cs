using gift_of_the_givers.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace gift_of_the_givers.Data
{
    public class GiftOfTheGiversDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public GiftOfTheGiversDbContext(
            DbContextOptions<GiftOfTheGiversDbContext> options)
            : base(options)
        {
        }

        public DbSet<Volunteer> Volunteers { get; set; }

        public DbSet<Donation> Donations { get; set; }

        public DbSet<ReliefProject> ReliefProjects { get; set; }

        public DbSet<ProjectUpdate> ProjectUpdates { get; set; }

        public DbSet<VolunteerAssignment> VolunteerAssignments { get; set; }

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(u => u.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(u => u.CreatedAt)
                    .IsRequired();
            });

            // Volunteer
            builder.Entity<Volunteer>(entity =>
            {
                entity.HasKey(v => v.VolunteerId);

                entity.Property(v => v.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(v => v.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(v => v.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(v => v.PhoneNumber)
                    .HasMaxLength(30);

                entity.Property(v => v.Skills)
                    .IsRequired();

                entity.Property(v => v.Availability)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(v => v.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(v => v.Status)
                    .IsRequired();

                entity.Property(v => v.RegistrationDate)
                    .IsRequired();

                entity.HasOne(v => v.User)
                    .WithOne(u => u.Volunteer)
                    .HasForeignKey<Volunteer>(v => v.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(v => v.Email);

                entity.HasIndex(v => v.Status);

                entity.HasIndex(v => v.RegistrationDate);
            });

            // Donation
            builder.Entity<Donation>(entity =>
            {
                entity.HasKey(d => d.DonationId);

                entity.Property(d => d.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(d => d.Currency)
                    .IsRequired();

                entity.Property(d => d.DonationType)
                    .IsRequired();

                entity.Property(d => d.DonationDate)
                    .IsRequired();

                entity.Property(d => d.TaxCertificateReference)
                    .HasMaxLength(100);

                entity.HasOne(d => d.User)
                    .WithMany(u => u.Donations)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(d => d.UserId);
                entity.HasIndex(d => d.DonationDate);
            });

            // Relief Project
            builder.Entity<ReliefProject>(entity =>
            {
                entity.HasKey(p => p.ReliefProjectId);

                entity.Property(p => p.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.Description)
                    .IsRequired();

                entity.Property(p => p.Location)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.CreatedBy)
                    .IsRequired();

                entity.HasOne(p => p.CreatedByUser)
                    .WithMany(u => u.ReliefProjectsCreated)
                    .HasForeignKey(p => p.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.Location);
                entity.HasIndex(p => p.StartDate);
                entity.HasIndex(p => p.CreatedBy);
            });

            // Project Update
            builder.Entity<ProjectUpdate>(entity =>
            {
                entity.HasKey(p => p.ProjectUpdateId);

                entity.Property(p => p.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.Description)
                    .IsRequired();

                entity.HasOne(p => p.ReliefProject)
                    .WithMany(p => p.ProjectUpdates)
                    .HasForeignKey(p => p.ReliefProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Employee)
                    .WithMany(u => u.ProjectUpdates)
                    .HasForeignKey(p => p.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.ReliefProjectId);
                entity.HasIndex(p => p.EmployeeId);
            });

            // Volunteer Assignment
            builder.Entity<VolunteerAssignment>(entity =>
            {
                entity.HasKey(a => a.VolunteerAssignmentId);

                entity.Property(a => a.Role)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne(a => a.Volunteer)
                    .WithMany(v => v.Assignments)
                    .HasForeignKey(a => a.VolunteerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.ReliefProject)
                    .WithMany(p => p.VolunteerAssignments)
                    .HasForeignKey(a => a.ReliefProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(a => a.VolunteerId);
                entity.HasIndex(a => a.ReliefProjectId);
            });
        }
    }
}