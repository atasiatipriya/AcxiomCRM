using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCrm.Controllers.Api;

[Route("api/auth")]
[ApiController]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiLoginResponse>> Login([FromBody] ApiLoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user != null && !user.IsActive)
        {
            await _auditService.LogAsync(user.Id, "FailedLogin", "Auth", user.Id, null, "Attempted API login on deactivated account.", ip);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiLoginResponse
            {
                Success = false,
                Message = "Your account has been deactivated. Please contact the administrator."
            });
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: true);

        if (result.Succeeded && user != null)
        {
            await _auditService.LogAsync(user.Id, "Login", "Auth", user.Id, null, "User logged in via REST API.", ip);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new ApiLoginResponse
            {
                Success = true,
                Message = "Authentication successful.",
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = roles.FirstOrDefault()
            });
        }

        if (result.IsLockedOut)
        {
            if (user != null)
            {
                await _auditService.LogAsync(user.Id, "Lockout", "Auth", user.Id, null, "Account locked out after repeated failed API attempts.", ip);
            }
            return StatusCode(StatusCodes.Status423Locked, new ApiLoginResponse
            {
                Success = false,
                Message = "Account locked out due to multiple failed login attempts. Please try again after 15 minutes."
            });
        }

        if (user != null)
        {
            await _auditService.LogAsync(user.Id, "FailedLogin", "Auth", user.Id, null, "Invalid API password attempt.", ip);
        }

        return Unauthorized(new ApiLoginResponse
        {
            Success = false,
            Message = "Invalid email or password."
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await _userManager.GetUserAsync(User);

        await _signInManager.SignOutAsync();

        if (user != null)
        {
            await _auditService.LogAsync(user.Id, "Logout", "Auth", user.Id, null, "User logged out via REST API.", ip);
        }

        return Ok(new { message = "Logged out successfully." });
    }
}
