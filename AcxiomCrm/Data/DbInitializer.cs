using AcxiomCrm.Models;
using AcxiomCrm.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var auditService = serviceProvider.GetRequiredService<IAuditService>();

        string[] roleNames = { "Admin", "Manager", "SalesExecutive" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var seedUsers = new[]
        {
            new
            {
                Email = "admin@acxiomcrm.local",
                FullName = "System Administrator",
                Password = "Admin@12345",
                Role = "Admin"
            },
            new
            {
                Email = "manager@acxiomcrm.local",
                FullName = "Sales Manager",
                Password = "Manager@12345",
                Role = "Manager"
            },
            new
            {
                Email = "sales@acxiomcrm.local",
                FullName = "Sales Executive",
                Password = "Sales@12345",
                Role = "SalesExecutive"
            }
        };

        foreach (var seed in seedUsers)
        {
            var user = await userManager.FindByEmailAsync(seed.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    FullName = seed.FullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, seed.Password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, seed.Role);
                }
            }
        }

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        if (!context.Customers.Any())
        {
            var seedCustomers = new[]
            {
                new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "Reliance Retail",
                    Email = "contact@relianceretail.com",
                    Phone = "9820011223",
                    CompanyName = "Reliance Industries",
                    Address = "Reliance Corporate Park, Thane-Belapur Road",
                    City = "Navi Mumbai",
                    State = "Maharashtra",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    CreatedBy = "admin@acxiomcrm.local"
                },
                new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Infosys Technologies",
                    Email = "procure@infosys-demo.com",
                    Phone = "9845012345",
                    CompanyName = "Infosys Ltd",
                    Address = "Electronics City, Hosur Road",
                    City = "Bengaluru",
                    State = "Karnataka",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    CreatedBy = "manager@acxiomcrm.local"
                },
                new Customer
                {
                    CustomerCode = "CUST-1003",
                    CustomerName = "Tech Mahindra Solutions",
                    Email = "reach@techmahindra-demo.com",
                    Phone = "9819098765",
                    CompanyName = "Tech Mahindra",
                    Address = "Sharda Centre, Erandwane",
                    City = "Pune",
                    State = "Maharashtra",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    CreatedBy = "sales@acxiomcrm.local"
                }
            };

            context.Customers.AddRange(seedCustomers);
            await context.SaveChangesAsync();
        }

        if (!context.Leads.Any())
        {
            var seedLeads = new[]
            {
                new Lead
                {
                    LeadCode = "LEAD-2001",
                    LeadName = "Arun Verma",
                    Email = "arun.verma@apexsystems.in",
                    Phone = "9821122334",
                    CompanyName = "Apex Systems",
                    Source = "Website",
                    Status = "Qualified",
                    ExpectedValue = 250000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    AssignedTo = "sales@acxiomcrm.local"
                },
                new Lead
                {
                    LeadCode = "LEAD-2002",
                    LeadName = "Sunita Rao",
                    Email = "sunita@zenithcloud.com",
                    Phone = "9845566778",
                    CompanyName = "Zenith Cloud Solutions",
                    Source = "Referral",
                    Status = "Contacted",
                    ExpectedValue = 450000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-7),
                    AssignedTo = "sales@acxiomcrm.local"
                },
                new Lead
                {
                    LeadCode = "LEAD-2003",
                    LeadName = "Pooja Patel",
                    Email = "pooja@orbitlogistics.org",
                    Phone = "9876655443",
                    CompanyName = "Orbit Logistics",
                    Source = "Advertisement",
                    Status = "New",
                    ExpectedValue = 180000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    AssignedTo = "manager@acxiomcrm.local"
                }
            };

            context.Leads.AddRange(seedLeads);
            await context.SaveChangesAsync();
        }

        if (!context.FollowUps.Any())
        {
            var firstCustomer = await context.Customers.FirstOrDefaultAsync();
            var firstLead = await context.Leads.FirstOrDefaultAsync();

            var seedFollowUps = new[]
            {
                new FollowUp
                {
                    CustomerId = firstCustomer?.CustomerId,
                    FollowUpDate = DateTime.Today.AddHours(14),
                    FollowUpType = "Call",
                    Remarks = "Discuss annual CRM license renewal and service contract",
                    Status = "Planned",
                    AssignedTo = "sales@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new FollowUp
                {
                    LeadId = firstLead?.LeadId,
                    FollowUpDate = DateTime.Today.AddDays(2).AddHours(11),
                    FollowUpType = "Demo",
                    Remarks = "Product walkthrough with technical architecture team",
                    Status = "Planned",
                    AssignedTo = "sales@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new FollowUp
                {
                    CustomerId = firstCustomer?.CustomerId,
                    FollowUpDate = DateTime.Today.AddDays(-2).AddHours(15),
                    FollowUpType = "Meeting",
                    Remarks = "Q3 pipeline review and procurement clearance",
                    Status = "Planned",
                    AssignedTo = "manager@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-5)
                }
            };

            context.FollowUps.AddRange(seedFollowUps);
            await context.SaveChangesAsync();
        }

        if (!context.Opportunities.Any())
        {
            var customers = await context.Customers.ToListAsync();
            var firstLead = await context.Leads.FirstOrDefaultAsync();

            var seedOpportunities = new[]
            {
                new Opportunity
                {
                    OpportunityName = "Reliance ERP Expansion",
                    CustomerId = customers.ElementAtOrDefault(0)?.CustomerId,
                    Amount = 1200000m,
                    Stage = "Negotiation",
                    Probability = 75,
                    ExpectedCloseDate = DateTime.Today.AddDays(25),
                    Status = "Open",
                    AssignedTo = "sales@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                },
                new Opportunity
                {
                    OpportunityName = "Infosys Cloud Migration",
                    CustomerId = customers.ElementAtOrDefault(1)?.CustomerId,
                    Amount = 850000m,
                    Stage = "Proposal",
                    Probability = 50,
                    ExpectedCloseDate = DateTime.Today.AddDays(40),
                    Status = "Open",
                    AssignedTo = "sales@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-10)
                },
                new Opportunity
                {
                    OpportunityName = "Tech Mahindra CRM Rollout",
                    CustomerId = customers.ElementAtOrDefault(2)?.CustomerId,
                    Amount = 600000m,
                    Stage = "Won",
                    Probability = 100,
                    ExpectedCloseDate = DateTime.Today.AddDays(-5),
                    Status = "Won",
                    AssignedTo = "manager@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                },
                new Opportunity
                {
                    OpportunityName = "Apex Analytics Platform",
                    LeadId = firstLead?.LeadId,
                    Amount = 350000m,
                    Stage = "Qualification",
                    Probability = 25,
                    ExpectedCloseDate = DateTime.Today.AddDays(60),
                    Status = "Open",
                    AssignedTo = "sales@acxiomcrm.local",
                    CreatedDate = DateTime.UtcNow.AddDays(-4)
                }
            };

            context.Opportunities.AddRange(seedOpportunities);
            await context.SaveChangesAsync();
        }

        if (!context.Activities.Any())
        {
            var customers = await context.Customers.ToListAsync();
            var firstLead = await context.Leads.FirstOrDefaultAsync();

            var seedActivities = new[]
            {
                new Activity
                {
                    ActivityType = "Call",
                    Subject = "Discovery call on CRM migration",
                    Description = "Spoke with procurement lead regarding timeline, user seat count, and legacy database migration requirements.",
                    ActivityDate = DateTime.Now.AddDays(-3).AddHours(-2),
                    CustomerId = customers.ElementAtOrDefault(0)?.CustomerId,
                    AssignedTo = "sales@acxiomcrm.local",
                    Status = "Completed",
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                },
                new Activity
                {
                    ActivityType = "Meeting",
                    Subject = "Stakeholder solution demo",
                    Description = "Conducted a 45-minute virtual architecture presentation with the VP of Engineering and sales team managers.",
                    ActivityDate = DateTime.Now.AddDays(-2),
                    LeadId = firstLead?.LeadId,
                    AssignedTo = "sales@acxiomcrm.local",
                    Status = "Completed",
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new Activity
                {
                    ActivityType = "Email",
                    Subject = "Sent proposal and pricing schedule",
                    Description = "Shared commercial breakdown, SLA terms, and compliance documentation via email for review.",
                    ActivityDate = DateTime.Now.AddDays(-1).AddHours(4),
                    CustomerId = customers.ElementAtOrDefault(1)?.CustomerId,
                    AssignedTo = "sales@acxiomcrm.local",
                    Status = "Completed",
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new Activity
                {
                    ActivityType = "Task",
                    Subject = "Prepare technical feasibility report",
                    Description = "Document API connectivity requirements and single sign-on integration details before contract finalizing.",
                    ActivityDate = DateTime.Now.AddDays(1).AddHours(2),
                    LeadId = firstLead?.LeadId,
                    AssignedTo = "sales@acxiomcrm.local",
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                }
            };

            context.Activities.AddRange(seedActivities);
            await context.SaveChangesAsync();
        }
    }
}
