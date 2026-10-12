using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.FollowUps;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Services;

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public FollowUpService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<FollowUpListViewModel> GetFollowUpsAsync(string? quickFilter = null, string? status = null, string? type = null)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .AsQueryable();

        if (_currentUser.IsSalesExecutive)
        {
            var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
            query = query.Where(f => f.AssignedTo == userIdentifier || f.AssignedTo == _currentUser.UserId || f.AssignedTo == null);
        }

        var today = DateTime.Today;

        var allList = await query.ToListAsync();
        var todayCount = allList.Count(f => f.FollowUpDate.Date == today && f.Status == "Planned");
        var upcomingCount = allList.Count(f => f.FollowUpDate.Date > today && f.Status == "Planned");
        var overdueCount = allList.Count(f => f.FollowUpDate.Date < today && f.Status == "Planned");

        var filtered = allList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(quickFilter))
        {
            filtered = quickFilter.ToLower() switch
            {
                "today" => filtered.Where(f => f.FollowUpDate.Date == today && f.Status == "Planned"),
                "upcoming" => filtered.Where(f => f.FollowUpDate.Date > today && f.Status == "Planned"),
                "overdue" => filtered.Where(f => f.FollowUpDate.Date < today && f.Status == "Planned"),
                "completed" => filtered.Where(f => f.Status == "Completed"),
                _ => filtered
            };
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(f => f.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            filtered = filtered.Where(f => f.FollowUpType == type);
        }

        return new FollowUpListViewModel
        {
            QuickFilter = quickFilter ?? "all",
            StatusFilter = status,
            TypeFilter = type,
            TodayCount = todayCount,
            UpcomingCount = upcomingCount,
            OverdueCount = overdueCount,
            FollowUps = filtered.OrderBy(f => f.FollowUpDate).ToList()
        };
    }

    public async Task<FollowUp?> GetByIdAsync(int id)
    {
        var followUp = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (followUp == null) return null;

        if (!await CanUserAccessFollowUpAsync(id))
        {
            return null;
        }

        return followUp;
    }

    public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(FollowUpFormViewModel model)
    {
        if (model.FollowUpDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.", null);
        }

        if (!model.CustomerId.HasValue && !model.LeadId.HasValue)
        {
            return (false, "Please select either a Customer or a Lead for this follow-up.", null);
        }

        var assigned = model.AssignedTo;
        if (string.IsNullOrWhiteSpace(assigned))
        {
            assigned = _currentUser.UserName ?? _currentUser.UserId;
        }

        var followUp = new FollowUp
        {
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Remarks = model.Remarks?.Trim(),
            Status = string.IsNullOrWhiteSpace(model.Status) ? "Planned" : model.Status,
            AssignedTo = assigned,
            CreatedDate = DateTime.UtcNow
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Create",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            $"Scheduled {followUp.FollowUpType} follow-up for {followUp.FollowUpDate:g}",
            _currentUser.IpAddress);

        return (true, null, followUp);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(FollowUpFormViewModel model)
    {
        if (!await CanUserAccessFollowUpAsync(model.FollowUpId))
        {
            return (false, "You do not have permission to modify this follow-up.");
        }

        var followUp = await _context.FollowUps.FindAsync(model.FollowUpId);
        if (followUp == null)
        {
            return (false, "Follow-up not found.");
        }

        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.");
        }

        var oldValue = $"Date: {followUp.FollowUpDate:g}, Status: {followUp.Status}, Type: {followUp.FollowUpType}";

        followUp.CustomerId = model.CustomerId;
        followUp.LeadId = model.LeadId;
        followUp.FollowUpDate = model.FollowUpDate;
        followUp.FollowUpType = model.FollowUpType;
        followUp.Remarks = model.Remarks?.Trim();
        followUp.Status = model.Status;

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            followUp.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValue = $"Date: {followUp.FollowUpDate:g}, Status: {followUp.Status}, Type: {followUp.FollowUpType}";

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Update",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            oldValue,
            newValue,
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? remarks)
    {
        if (!await CanUserAccessFollowUpAsync(id))
        {
            return (false, "You do not have permission to complete this follow-up.");
        }

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return (false, "Follow-up not found.");
        }

        var previousStatus = followUp.Status;
        followUp.Status = "Completed";

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? remarks.Trim()
                : $"{followUp.Remarks} | Completed note: {remarks.Trim()}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Complete",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            $"Status: {previousStatus}",
            $"Status: Completed",
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks)
    {
        if (!await CanUserAccessFollowUpAsync(id))
        {
            return (false, "You do not have permission to reschedule this follow-up.");
        }

        if (newDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.");
        }

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return (false, "Follow-up not found.");
        }

        var oldDate = followUp.FollowUpDate;
        followUp.FollowUpDate = newDate;
        followUp.Status = "Planned";

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? remarks.Trim()
                : $"{followUp.Remarks} | Rescheduled: {remarks.Trim()}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Reschedule",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            $"Date: {oldDate:g}",
            $"Date: {newDate:g}",
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<bool> CanUserAccessFollowUpAsync(int id)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return true;
        }

        var followUp = await _context.FollowUps.AsNoTracking().FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null) return false;

        var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
        return followUp.AssignedTo == userIdentifier || followUp.AssignedTo == _currentUser.UserId || followUp.AssignedTo == null;
    }
}
