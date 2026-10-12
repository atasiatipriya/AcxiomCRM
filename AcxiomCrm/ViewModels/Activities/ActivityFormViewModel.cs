using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.ViewModels.Activities;

public class ActivityFormViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity Type is required.")]
    [StringLength(50)]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call";

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    [Display(Name = "Subject / Title")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description / Summary")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Activity Date & Time")]
    public DateTime ActivityDate { get; set; } = DateTime.Now;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Completed";
}
