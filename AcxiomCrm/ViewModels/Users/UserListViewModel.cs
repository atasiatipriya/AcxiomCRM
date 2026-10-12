namespace AcxiomCrm.ViewModels.Users;

public class UserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class UserListViewModel
{
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
    public IEnumerable<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();
}
