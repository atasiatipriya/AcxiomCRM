using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.ViewModels.Leads;

public class LeadConvertViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Create Initial Opportunity")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    [StringLength(100)]
    public string? OpportunityName { get; set; }

    [Display(Name = "Opportunity Amount")]
    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpectedCloseDate { get; set; } = DateTime.Today.AddMonths(1);
}
