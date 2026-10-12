using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.ViewModels.Customers;

public class CustomerFormViewModel
{
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    [StringLength(50)]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

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

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Active";
}
