using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string dateFilter = "All")
    {
        var now = DateTime.UtcNow;
        var today = DateTime.Today;

        // Base scoped queries
        var customerQuery = _context.Customers.AsQueryable();
        var leadQuery = _context.Leads.AsQueryable();
        var oppQuery = _context.Opportunities.Include(o => o.Customer).AsQueryable();
        var followUpQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsQueryable();

        if (_currentUser.IsSalesExecutive)
        {
            var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
            customerQuery = customerQuery.Where(c => c.CreatedBy == userIdentifier || c.CreatedBy == _currentUser.UserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == userIdentifier || l.AssignedTo == _currentUser.UserId || l.AssignedTo == null);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userIdentifier || o.AssignedTo == _currentUser.UserId);
            followUpQuery = followUpQuery.Where(f => f.AssignedTo == userIdentifier || f.AssignedTo == _currentUser.UserId);
        }

        // Apply date filter
        DateTime? filterStart = dateFilter switch
        {
            "Today" => today,
            "ThisWeek" => today.AddDays(-(int)today.DayOfWeek),
            "ThisMonth" => new DateTime(today.Year, today.Month, 1),
            _ => null
        };

        if (filterStart.HasValue)
        {
            var startUtc = filterStart.Value.ToUniversalTime();
            customerQuery = customerQuery.Where(c => c.CreatedDate >= startUtc);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startUtc);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startUtc);
        }

        // Execute counts and sums
        var totalCustomers = await customerQuery.CountAsync();
        var allLeads = await leadQuery.ToListAsync();
        var allOpps = await oppQuery.ToListAsync();
        var allFollowUps = await followUpQuery.ToListAsync();

        var totalLeads = allLeads.Count;
        var openLeads = allLeads.Count(l => !string.Equals(l.Status, "Converted", StringComparison.OrdinalIgnoreCase) &&
                                            !string.Equals(l.Status, "Lost", StringComparison.OrdinalIgnoreCase));
        var convertedLeads = allLeads.Count(l => string.Equals(l.Status, "Converted", StringComparison.OrdinalIgnoreCase));

        var totalOpps = allOpps.Count;
        var openOpps = allOpps.Count(o => string.Equals(o.Status, "Open", StringComparison.OrdinalIgnoreCase));
        var wonOpps = allOpps.Count(o => string.Equals(o.Stage, "Won", StringComparison.OrdinalIgnoreCase) || string.Equals(o.Status, "Won", StringComparison.OrdinalIgnoreCase));
        var lostOpps = allOpps.Count(o => string.Equals(o.Stage, "Lost", StringComparison.OrdinalIgnoreCase) || string.Equals(o.Status, "Lost", StringComparison.OrdinalIgnoreCase));

        var openOpportunities = allOpps.Where(o => string.Equals(o.Status, "Open", StringComparison.OrdinalIgnoreCase)).ToList();
        var totalPipelineValue = openOpportunities.Sum(o => o.Amount);
        var weightedPipelineValue = openOpportunities.Sum(o => o.WeightedValue);

        var pendingFollowUps = allFollowUps.Count(f => string.Equals(f.Status, "Planned", StringComparison.OrdinalIgnoreCase));
        var completedFollowUps = allFollowUps.Count(f => string.Equals(f.Status, "Completed", StringComparison.OrdinalIgnoreCase));
        var overdueFollowUps = allFollowUps.Count(f => string.Equals(f.Status, "Planned", StringComparison.OrdinalIgnoreCase) && f.FollowUpDate.Date < today);

        // Chart 1: Lead Status Doughnut Chart
        var leadStatuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
        var leadStatusCounts = leadStatuses.Select(s => allLeads.Count(l => string.Equals(l.Status, s, StringComparison.OrdinalIgnoreCase))).ToList();

        // Chart 2: Opportunity Pipeline Bar Chart
        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var stageAmounts = stages.Select(s => allOpps.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).Sum(o => o.Amount)).ToList();
        var stageCounts = stages.Select(s => allOpps.Count(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase))).ToList();

        // Chart 3: Monthly Sales / Opportunity Trend (Last 6 Months)
        var monthlyLabels = new List<string>();
        var monthlyAmounts = new List<decimal>();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = today.AddMonths(-i);
            var label = targetMonth.ToString("MMM yyyy");
            monthlyLabels.Add(label);

            var monthTotal = allOpps
                .Where(o => o.CreatedDate.Year == targetMonth.Year && o.CreatedDate.Month == targetMonth.Month)
                .Sum(o => o.Amount);

            monthlyAmounts.Add(monthTotal);
        }

        // Recent items
        var recentLeads = allLeads.OrderByDescending(l => l.CreatedDate).Take(5).ToList();
        var recentOpps = allOpps.OrderByDescending(o => o.CreatedDate).Take(5).ToList();
        var upcomingFollowUps = allFollowUps
            .Where(f => string.Equals(f.Status, "Planned", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToList();

        var viewModel = new DashboardViewModel
        {
            DateFilter = dateFilter,
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            ConvertedLeads = convertedLeads,
            TotalOpportunities = totalOpps,
            OpenOpportunities = openOpps,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            PendingFollowUps = pendingFollowUps,
            CompletedFollowUps = completedFollowUps,
            OverdueFollowUps = overdueFollowUps,
            LeadStatusLabels = leadStatuses.ToList(),
            LeadStatusCounts = leadStatusCounts,
            PipelineStageLabels = stages.ToList(),
            PipelineStageAmounts = stageAmounts,
            PipelineStageCounts = stageCounts,
            MonthlySalesLabels = monthlyLabels,
            MonthlySalesAmounts = monthlyAmounts,
            RecentLeads = recentLeads,
            RecentOpportunities = recentOpps,
            UpcomingFollowUps = upcomingFollowUps
        };

        return View(viewModel);
    }
}
