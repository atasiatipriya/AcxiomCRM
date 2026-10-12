using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize]
public class ActivityController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public ActivityController(
        IActivityService activityService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser)
    {
        _activityService = activityService;
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? type, string? status)
    {
        var model = await _activityService.GetActivitiesAsync(search, type, status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null)
        {
            TempData["ErrorMessage"] = "Activity not found or access denied.";
            return RedirectToAction(nameof(Index));
        }
        return View(activity);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId, string? type)
    {
        await PopulateDropdownsAsync();
        var model = new ActivityFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityType = !string.IsNullOrWhiteSpace(type) ? type : "Call",
            ActivityDate = DateTime.Now,
            AssignedTo = _currentUser.UserName
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var result = await _activityService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to log activity.");
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"{model.ActivityType} activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null)
        {
            TempData["ErrorMessage"] = "Activity not found or access denied.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync();
        var model = new ActivityFormViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            Status = activity.Status,
            AssignedTo = activity.AssignedTo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var result = await _activityService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update activity.");
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Activity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var result = await _activityService.ToggleStatusAsync(id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to update activity status.";
        }
        else
        {
            TempData["SuccessMessage"] = "Activity status updated.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => l.Status != "Converted").OrderBy(l => l.LeadName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName");
        ViewBag.Users = new SelectList(users, "UserName", "FullName");
    }
}
