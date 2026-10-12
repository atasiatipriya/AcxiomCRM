namespace AcxiomCrm.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string Role { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsSalesExecutive { get; }
    string? IpAddress { get; }
}
