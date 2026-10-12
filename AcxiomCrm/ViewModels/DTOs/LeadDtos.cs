using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.ViewModels.DTOs;

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "New";
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; }
}

public class LeadCreateDto
{
    [StringLength(50)]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CompanyName { get; set; }

    [StringLength(50)]
    public string? Source { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(30)]
    public string Status { get; set; } = "New";

    [Required(ErrorMessage = "Expected value is required.")]
    [Range(0, 100000000, ErrorMessage = "Expected Value must be 0 or greater.")]
    public decimal ExpectedValue { get; set; }

    public string? AssignedTo { get; set; }
}
