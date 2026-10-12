using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCrm.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null && !user.IsActive)
        {
            await _auditService.LogAsync(user.Id, "FailedLogin", "Auth", user.Id, null, "Attempted login on inactive account.", ip);
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded && user != null)
        {
            await _auditService.LogAsync(user.Id, "Login", "Auth", user.Id, null, "User logged in successfully.", ip);
            _logger.LogInformation("User {Email} logged in.", model.Email);
            return RedirectToLocal(returnUrl);
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user?.Id ?? model.Email, "Lockout", "Auth", user?.Id, null, "User account locked out due to repeated failed logins.", ip);
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            ModelState.AddModelError(string.Empty, "This account has been locked out due to multiple failed login attempts. Please try again after 15 minutes.");
            return View(model);
        }

        await _auditService.LogAsync(user?.Id ?? model.Email, "FailedLogin", "Auth", user?.Id, null, "Invalid login credentials attempt.", ip);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "A user with this email address already exists.");
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var roleToAssign = string.IsNullOrWhiteSpace(model.Role) ? "SalesExecutive" : model.Role;
            await _userManager.AddToRoleAsync(user, roleToAssign);

            await _auditService.LogAsync(user.Id, "Register", "User", user.Id, null, $"User registered with role {roleToAssign}.", ip);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(userId))
        {
            await _auditService.LogAsync(userId, "Logout", "Auth", userId, null, "User logged out.", ip);
        }

        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Dashboard");
    }
}
