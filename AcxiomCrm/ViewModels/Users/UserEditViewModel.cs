using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.ViewModels.Users;

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [Display(Name = "Assigned Role")]
    public string Role { get; set; } = "SalesExecutive";

    [Display(Name = "Active Account")]
    public bool IsActive { get; set; }

    [StringLength(100, ErrorMessage = "New password must be at least {2} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Reset Password (leave blank to keep current)")]
    public string? NewPassword { get; set; }
}
