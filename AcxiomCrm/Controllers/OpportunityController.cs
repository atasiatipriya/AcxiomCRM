using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize]
public class OpportunityController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public OpportunityController(
        IOpportunityService opportunityService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser)
    {
        _opportunityService = opportunityService;
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? stage, string? status)
    {
        var model = await _opportunityService.GetOpportunitiesAsync(search, stage, status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or access denied.";
            return RedirectToAction(nameof(Index));
        }
        return View(opp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        await PopulateDropdownsAsync();
        var model = new OpportunityFormViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ExpectedCloseDate = DateTime.Today.AddMonths(1),
            AssignedTo = _currentUser.UserName
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityFormViewModel model)
    {
        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
        }

        if (model.Status != "Lost" && model.Status != "Won" && model.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var result = await _opportunityService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create opportunity.");
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' added to pipeline.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or access denied.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync();
        var model = new OpportunityFormViewModel
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OpportunityFormViewModel model)
    {
        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
        }

        if (model.Status != "Lost" && model.Status != "Won" && model.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var result = await _opportunityService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update opportunity.");
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStage(int id, string newStage)
    {
        var result = await _opportunityService.UpdateStageAsync(id, newStage);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to update stage.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Opportunity stage updated to '{newStage}'.";
        }

        return RedirectToAction(nameof(Details), new { id });
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
