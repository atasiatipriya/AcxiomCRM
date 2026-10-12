using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.ViewModels.Leads;

public class LeadFormViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    [StringLength(50)]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone Number (10-digit)")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [StringLength(50)]
    [Display(Name = "Lead Source")]
    public string? Source { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(30)]
    public string Status { get; set; } = "New";

    [Required(ErrorMessage = "Expected value is required.")]
    [Range(0, 100000000, ErrorMessage = "Expected Value must be 0 or greater.")]
    [Display(Name = "Expected Value (INR)")]
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Assign To")]
    public string? AssignedTo { get; set; }
}
