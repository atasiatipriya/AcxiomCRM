using System.Security.Claims;

namespace AcxiomCrm.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User?.Identity?.Name;

    public string Role
    {
        get
        {
            if (User == null) return "Anonymous";
            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Manager")) return "Manager";
            if (User.IsInRole("SalesExecutive")) return "SalesExecutive";
            return "User";
        }
    }

    public bool IsAdmin => User?.IsInRole("Admin") ?? false;

    public bool IsManager => User?.IsInRole("Manager") ?? false;

    public bool IsSalesExecutive => User?.IsInRole("SalesExecutive") ?? false;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
