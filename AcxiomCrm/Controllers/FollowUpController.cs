using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.FollowUps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize]
public class FollowUpController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public FollowUpController(
        IFollowUpService followUpService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser)
    {
        _followUpService = followUpService;
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? quickFilter, string? status, string? type)
    {
        var model = await _followUpService.GetFollowUpsAsync(quickFilter, status, type);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        await PopulateDropdownsAsync();
        var model = new FollowUpFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            FollowUpDate = DateTime.Today.AddHours(11),
            AssignedTo = _currentUser.UserName
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpFormViewModel model)
    {
        if (model.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var result = await _followUpService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to schedule follow-up.");
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await _followUpService.GetByIdAsync(id);
        if (followUp == null)
        {
            TempData["ErrorMessage"] = "Follow-up not found or access denied.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync();
        var model = new FollowUpFormViewModel
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FollowUpFormViewModel model)
    {
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var result = await _followUpService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update follow-up.");
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var result = await _followUpService.CompleteFollowUpAsync(id, remarks);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to complete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as Completed!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
    {
        var result = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to reschedule follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
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
