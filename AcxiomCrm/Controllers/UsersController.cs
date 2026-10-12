using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email!.ToLower().Contains(term));
        }

        var usersList = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var result = new List<UserItemViewModel>();

        foreach (var user in usersList)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(primaryRole, role, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            result.Add(new UserItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Role = primaryRole,
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                AccessFailedCount = user.AccessFailedCount,
                LockoutEnd = user.LockoutEnd,
                CreatedDate = user.CreatedDate
            });
        }

        var viewModel = new UserListViewModel
        {
            SearchTerm = search,
            RoleFilter = role,
            Users = result
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            IsActive = model.IsActive,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                _currentUser.UserId,
                "Create",
                "User",
                user.Id,
                null,
                $"Admin created user {user.Email} with role {model.Role}",
                _currentUser.IpAddress);

            TempData["SuccessMessage"] = $"User '{user.FullName}' ({model.Role}) created successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? "SalesExecutive",
            IsActive = user.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "None";
        user.FullName = model.FullName.Trim();
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        if (oldRole != model.Role)
        {
            await _userManager.RemoveFromRoleAsync(user, oldRole);
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                _currentUser.UserId,
                "RoleChange",
                "User",
                user.Id,
                $"Role: {oldRole}",
                $"Role: {model.Role}",
                _currentUser.IpAddress);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _auditService.LogAsync(
                _currentUser.UserId,
                "Security",
                "User",
                user.Id,
                null,
                "Admin reset password for user",
                _currentUser.IpAddress);
        }

        TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (id == _currentUser.UserId)
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Update",
            "User",
            user.Id,
            $"IsActive: {!user.IsActive}",
            $"IsActive: {user.IsActive}",
            _currentUser.IpAddress);

        TempData["SuccessMessage"] = $"User '{user.FullName}' status changed to {(user.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Security",
            "User",
            user.Id,
            "Locked Out",
            "Account Unlocked by Administrator",
            _currentUser.IpAddress);

        TempData["SuccessMessage"] = $"Account for '{user.FullName}' has been unlocked.";
        return RedirectToAction(nameof(Index));
    }
}
