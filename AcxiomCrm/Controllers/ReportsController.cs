using System.Text;
using AcxiomCrm.Data;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string reportType = "Pipeline", DateTime? fromDate = null, DateTime? toDate = null, string? statusFilter = null)
    {
        var model = await GenerateReportDataAsync(reportType, fromDate, toDate, statusFilter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string reportType = "Pipeline", DateTime? fromDate = null, DateTime? toDate = null, string? statusFilter = null)
    {
        var model = await GenerateReportDataAsync(reportType, fromDate, toDate, statusFilter);
        var sb = new StringBuilder();

        switch (reportType.ToLower())
        {
            case "customers":
                sb.AppendLine("CustomerCode,CustomerName,Email,Phone,CompanyName,Status,CreatedDate,CreatedBy");
                foreach (var r in model.CustomerRows)
                {
                    sb.AppendLine($"\"{EscapeCsv(r.CustomerCode)}\",\"{EscapeCsv(r.CustomerName)}\",\"{EscapeCsv(r.Email)}\",\"{EscapeCsv(r.Phone)}\",\"{EscapeCsv(r.CompanyName)}\",\"{EscapeCsv(r.Status)}\",\"{r.CreatedDate:yyyy-MM-dd}\",\"{EscapeCsv(r.CreatedBy)}\"");
                }
                break;

            case "leads":
                sb.AppendLine("LeadCode,LeadName,Email,Phone,Source,Status,ExpectedValue,CreatedDate,AssignedTo");
                foreach (var r in model.LeadRows)
                {
                    sb.AppendLine($"\"{EscapeCsv(r.LeadCode)}\",\"{EscapeCsv(r.LeadName)}\",\"{EscapeCsv(r.Email)}\",\"{EscapeCsv(r.Phone)}\",\"{EscapeCsv(r.Source)}\",\"{EscapeCsv(r.Status)}\",{r.ExpectedValue},\"{r.CreatedDate:yyyy-MM-dd}\",\"{EscapeCsv(r.AssignedTo)}\"");
                }
                break;

            case "followups":
                sb.AppendLine("FollowUpDate,Remarks,Type,Status,Target,AssignedTo");
                foreach (var r in model.FollowUpRows)
                {
                    sb.AppendLine($"\"{r.FollowUpDate:yyyy-MM-dd HH:mm}\",\"{EscapeCsv(r.Remarks)}\",\"{EscapeCsv(r.FollowUpType)}\",\"{EscapeCsv(r.Status)}\",\"{EscapeCsv(r.TargetName)}\",\"{EscapeCsv(r.AssignedTo)}\"");
                }
                break;

            case "pipeline":
            default:
                sb.AppendLine("OpportunityName,CustomerName,Amount,Stage,Probability,WeightedValue,ExpectedCloseDate,Status,AssignedTo");
                foreach (var r in model.OpportunityRows)
                {
                    sb.AppendLine($"\"{EscapeCsv(r.OpportunityName)}\",\"{EscapeCsv(r.CustomerName)}\",{r.Amount},\"{EscapeCsv(r.Stage)}\",{r.Probability},{r.WeightedValue},\"{r.ExpectedCloseDate:yyyy-MM-dd}\",\"{EscapeCsv(r.Status)}\",\"{EscapeCsv(r.AssignedTo)}\"");
                }
                break;
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"AcxiomCRM_{reportType}_Report_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private async Task<ReportsIndexViewModel> GenerateReportDataAsync(string reportType, DateTime? fromDate, DateTime? toDate, string? statusFilter)
    {
        var model = new ReportsIndexViewModel
        {
            ActiveReport = reportType,
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = statusFilter
        };

        var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";

        // Query bases
        var custQuery = _context.Customers.AsQueryable();
        var leadQuery = _context.Leads.AsQueryable();
        var oppQuery = _context.Opportunities.Include(o => o.Customer).AsQueryable();
        var fuQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsQueryable();

        // Scope
        if (_currentUser.IsSalesExecutive)
        {
            custQuery = custQuery.Where(c => c.CreatedBy == userIdentifier || c.CreatedBy == _currentUser.UserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == userIdentifier || l.AssignedTo == _currentUser.UserId || l.AssignedTo == null);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userIdentifier || o.AssignedTo == _currentUser.UserId);
            fuQuery = fuQuery.Where(f => f.AssignedTo == userIdentifier || f.AssignedTo == _currentUser.UserId);
        }

        // Summary counts
        model.TotalCustomers = await custQuery.CountAsync();
        model.TotalLeads = await leadQuery.CountAsync();
        model.TotalOpportunities = await oppQuery.CountAsync();
        model.TotalFollowUps = await fuQuery.CountAsync();
        model.TotalPipelineValue = await oppQuery.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0;
        var openOpps = await oppQuery.Where(o => o.Status == "Open").ToListAsync();
        model.TotalWeightedValue = openOpps.Sum(o => o.WeightedValue);

        // Date range filters
        if (fromDate.HasValue)
        {
            var start = fromDate.Value.Date;
            custQuery = custQuery.Where(c => c.CreatedDate >= start);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= start);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= start);
            fuQuery = fuQuery.Where(f => f.FollowUpDate >= start);
        }

        if (toDate.HasValue)
        {
            var end = toDate.Value.Date.AddDays(1);
            custQuery = custQuery.Where(c => c.CreatedDate < end);
            leadQuery = leadQuery.Where(l => l.CreatedDate < end);
            oppQuery = oppQuery.Where(o => o.CreatedDate < end);
            fuQuery = fuQuery.Where(f => f.FollowUpDate < end);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            custQuery = custQuery.Where(c => c.Status == statusFilter);
            leadQuery = leadQuery.Where(l => l.Status == statusFilter);
            oppQuery = oppQuery.Where(o => o.Stage == statusFilter || o.Status == statusFilter);
            fuQuery = fuQuery.Where(f => f.Status == statusFilter);
        }

        // Populate active report data
        switch (reportType.ToLower())
        {
            case "customers":
                model.CustomerRows = await custQuery
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new CustomerReportRow
                    {
                        CustomerCode = c.CustomerCode,
                        CustomerName = c.CustomerName,
                        Email = c.Email,
                        Phone = c.Phone,
                        CompanyName = c.CompanyName,
                        Status = c.Status,
                        CreatedDate = c.CreatedDate,
                        CreatedBy = c.CreatedBy
                    }).ToListAsync();
                break;

            case "leads":
                model.LeadRows = await leadQuery
                    .OrderByDescending(l => l.CreatedDate)
                    .Select(l => new LeadReportRow
                    {
                        LeadCode = l.LeadCode,
                        LeadName = l.LeadName,
                        Email = l.Email,
                        Phone = l.Phone,
                        Source = l.Source,
                        Status = l.Status,
                        ExpectedValue = l.ExpectedValue,
                        CreatedDate = l.CreatedDate,
                        AssignedTo = l.AssignedTo
                    }).ToListAsync();
                break;

            case "followups":
                model.FollowUpRows = await fuQuery
                    .OrderByDescending(f => f.FollowUpDate)
                    .Select(f => new FollowUpReportRow
                    {
                        FollowUpDate = f.FollowUpDate,
                        Remarks = f.Remarks,
                        FollowUpType = f.FollowUpType,
                        Status = f.Status,
                        TargetName = f.Customer != null ? f.Customer.CustomerName : (f.Lead != null ? f.Lead.LeadName : "General"),
                        AssignedTo = f.AssignedTo
                    }).ToListAsync();
                break;

            case "pipeline":
            default:
                var oppsList = await oppQuery.OrderByDescending(o => o.CreatedDate).ToListAsync();
                model.OpportunityRows = oppsList.Select(o => new OpportunityReportRow
                {
                    OpportunityName = o.OpportunityName,
                    CustomerName = o.Customer?.CustomerName,
                    Amount = o.Amount,
                    Stage = o.Stage,
                    Probability = o.Probability,
                    WeightedValue = o.WeightedValue,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    AssignedTo = o.AssignedTo
                }).ToList();
                break;
        }

        return model;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }
}
