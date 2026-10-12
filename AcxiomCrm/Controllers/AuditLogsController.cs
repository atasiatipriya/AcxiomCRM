using AcxiomCrm.Data;
using AcxiomCrm.ViewModels.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userId, string? actionType, string? entityName, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var userSearch = userId.Trim().ToLower();
            query = query.Where(a => a.UserId != null && a.UserId.ToLower().Contains(userSearch));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.Action == actionType);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var nextDay = toDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedDate < nextDay);
        }

        var total = await query.CountAsync();
        var logs = await query.OrderByDescending(a => a.CreatedDate).Take(150).ToListAsync();

        var viewModel = new AuditLogFilterViewModel
        {
            UserId = userId,
            Action = actionType,
            EntityName = entityName,
            FromDate = fromDate,
            ToDate = toDate,
            TotalLogs = total,
            Logs = logs
        };

        return View(viewModel);
    }
}
