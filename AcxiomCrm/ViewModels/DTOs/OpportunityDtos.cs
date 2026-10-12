using System.ComponentModel.DataAnnotations;
using AcxiomCrm.ViewModels.Opportunities;

namespace AcxiomCrm.ViewModels.DTOs;

public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = "Qualification";
    public int Probability { get; set; } = 10;
    public decimal WeightedValue { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; }
}

public class OpportunityCreateDto
{
    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(100, ErrorMessage = "Opportunity Name cannot exceed 100 characters.")]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [StringLength(50)]
    public string Stage { get; set; } = "Qualification";

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 10;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [NotPastDate(ErrorMessage = "Expected Close Date cannot be in the past.")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    public string Status { get; set; } = "Open";
    public string? AssignedTo { get; set; }
}
