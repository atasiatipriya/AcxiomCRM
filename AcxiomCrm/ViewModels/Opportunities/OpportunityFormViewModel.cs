using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCrm.ViewModels.Opportunities;

public class NotPastDateAttribute : ValidationAttribute, IClientModelValidator
{
    public NotPastDateAttribute()
        : base("Expected Close Date cannot be in the past.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is DateTime dt)
        {
            return dt.Date >= DateTime.Today;
        }
        return true;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (!context.Attributes.ContainsKey("data-val"))
        {
            context.Attributes.Add("data-val", "true");
        }
        if (!context.Attributes.ContainsKey("data-val-notpastdate"))
        {
            context.Attributes.Add("data-val-notpastdate", ErrorMessageString);
        }
    }
}

public class OpportunityFormViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(100, ErrorMessage = "Opportunity Name cannot exceed 100 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount (INR)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [StringLength(50)]
    [Display(Name = "Pipeline Stage")]
    public string Stage { get; set; } = "Qualification";

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Win Probability (%)")]
    public int Probability { get; set; } = 25;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [NotPastDate(ErrorMessage = "Expected Close Date cannot be in the past.")]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddMonths(1);

    [Required]
    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Open";

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
}
