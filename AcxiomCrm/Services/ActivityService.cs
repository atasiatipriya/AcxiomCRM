using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Activities;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Services;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ActivityService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<ActivityListViewModel> GetActivitiesAsync(string? search = null, string? type = null, string? status = null)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .AsQueryable();

        if (_currentUser.IsSalesExecutive)
        {
            var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
            query = query.Where(a => a.AssignedTo == userIdentifier || a.AssignedTo == _currentUser.UserId || a.AssignedTo == null);
        }

        var allList = await query.ToListAsync();

        var total = allList.Count;
        var calls = allList.Count(a => a.ActivityType == "Call");
        var meetings = allList.Count(a => a.ActivityType == "Meeting");
        var emails = allList.Count(a => a.ActivityType == "Email");
        var tasks = allList.Count(a => a.ActivityType == "Task");

        var filtered = allList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            filtered = filtered.Where(a =>
                a.Subject.ToLower().Contains(term) ||
                (a.Description != null && a.Description.ToLower().Contains(term)) ||
                (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)) ||
                (a.Lead != null && a.Lead.LeadName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            filtered = filtered.Where(a => a.ActivityType == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(a => a.Status == status);
        }

        return new ActivityListViewModel
        {
            SearchTerm = search,
            TypeFilter = type,
            StatusFilter = status,
            TotalActivities = total,
            CallsCount = calls,
            MeetingsCount = meetings,
            EmailsCount = emails,
            TasksCount = tasks,
            Activities = filtered.OrderByDescending(a => a.ActivityDate).ToList()
        };
    }

    public async Task<Activity?> GetByIdAsync(int id)
    {
        var activity = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

        if (activity == null) return null;

        if (!await CanUserAccessActivityAsync(id))
        {
            return null;
        }

        return activity;
    }

    public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateAsync(ActivityFormViewModel model)
    {
        var assigned = model.AssignedTo;
        if (string.IsNullOrWhiteSpace(assigned))
        {
            assigned = _currentUser.UserName ?? _currentUser.UserId;
        }

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject.Trim(),
            Description = model.Description?.Trim(),
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            AssignedTo = assigned,
            Status = string.IsNullOrWhiteSpace(model.Status) ? "Completed" : model.Status,
            CreatedDate = DateTime.UtcNow
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Create",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            $"Logged {activity.ActivityType}: {activity.Subject}",
            _currentUser.IpAddress);

        return (true, null, activity);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(ActivityFormViewModel model)
    {
        if (!await CanUserAccessActivityAsync(model.ActivityId))
        {
            return (false, "You do not have permission to modify this activity.");
        }

        var activity = await _context.Activities.FindAsync(model.ActivityId);
        if (activity == null)
        {
            return (false, "Activity not found.");
        }

        var oldValue = $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}";

        activity.ActivityType = model.ActivityType;
        activity.Subject = model.Subject.Trim();
        activity.Description = model.Description?.Trim();
        activity.ActivityDate = model.ActivityDate;
        activity.CustomerId = model.CustomerId;
        activity.LeadId = model.LeadId;
        activity.Status = model.Status;

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            activity.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValue = $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}";

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Update",
            "Activity",
            activity.ActivityId.ToString(),
            oldValue,
            newValue,
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> ToggleStatusAsync(int id)
    {
        if (!await CanUserAccessActivityAsync(id))
        {
            return (false, "You do not have permission to update this activity.");
        }

        var activity = await _context.Activities.FindAsync(id);
        if (activity == null)
        {
            return (false, "Activity not found.");
        }

        var prev = activity.Status;
        activity.Status = activity.Status == "Completed" ? "Pending" : "Completed";

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "UpdateStatus",
            "Activity",
            activity.ActivityId.ToString(),
            $"Status: {prev}",
            $"Status: {activity.Status}",
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<bool> CanUserAccessActivityAsync(int id)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return true;
        }

        var activity = await _context.Activities.AsNoTracking().FirstOrDefaultAsync(a => a.ActivityId == id);
        if (activity == null) return false;

        var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
        return activity.AssignedTo == userIdentifier || activity.AssignedTo == _currentUser.UserId || activity.AssignedTo == null;
    }
}
