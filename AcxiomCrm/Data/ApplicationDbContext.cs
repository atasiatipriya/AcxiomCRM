using AcxiomCrm.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.CustomerCode).IsUnique();
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.Phone).IsUnique();

            entity.HasMany(c => c.Opportunities)
                  .WithOne(o => o.Customer)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.FollowUps)
                  .WithOne(f => f.Customer)
                  .HasForeignKey(f => f.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Activities)
                  .WithOne(a => a.Customer)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Lead>(entity =>
        {
            entity.HasIndex(l => l.LeadCode).IsUnique();

            entity.Property(l => l.ExpectedValue)
                  .HasPrecision(18, 2);

            entity.HasMany(l => l.Opportunities)
                  .WithOne(o => o.Lead)
                  .HasForeignKey(o => o.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(l => l.FollowUps)
                  .WithOne(f => f.Lead)
                  .HasForeignKey(f => f.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(l => l.Activities)
                  .WithOne(a => a.Lead)
                  .HasForeignKey(a => a.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Opportunity>(entity =>
        {
            entity.Property(o => o.Amount)
                  .HasPrecision(18, 2);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.CreatedDate);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.EntityName);
        });
    }
}
