using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCrm.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public LeadController(
        ILeadService leadService,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser)
    {
        _leadService = leadService;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, string? source)
    {
        var leads = await _leadService.GetLeadsAsync(search, status, source);
        var viewModel = new LeadListViewModel
        {
            SearchTerm = search,
            StatusFilter = status,
            SourceFilter = source,
            Leads = leads
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or access denied.";
            return RedirectToAction(nameof(Index));
        }
        return View(lead);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new LeadFormViewModel
        {
            AssignedTo = _currentUser.UserName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        var result = await _leadService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create lead.");
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{model.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or access denied.";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(lead.Status, "Converted", StringComparison.OrdinalIgnoreCase))
        {
            TempData["WarningMessage"] = "Converted leads cannot be modified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PopulateUsersDropdownAsync();
        var model = new LeadFormViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeadFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        var result = await _leadService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update lead.");
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{model.LeadName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string newStatus)
    {
        var result = await _leadService.UpdateStatusAsync(id, newStatus);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to update lead status.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Lead status updated to '{newStatus}'.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or access denied.";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(lead.Status, "Converted", StringComparison.OrdinalIgnoreCase))
        {
            TempData["WarningMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            CustomerName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            CreateOpportunity = true,
            OpportunityName = $"{lead.LeadName} - Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 50000m,
            ExpectedCloseDate = DateTime.Today.AddMonths(1)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _leadService.ConvertLeadAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to convert lead.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Lead converted into Customer successfully!";
        if (result.CustomerId.HasValue)
        {
            return RedirectToAction("Details", "Customer", new { id = result.CustomerId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync()
    {
        var users = _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
        ViewBag.UsersList = new SelectList(users, "UserName", "FullName");
    }
}
