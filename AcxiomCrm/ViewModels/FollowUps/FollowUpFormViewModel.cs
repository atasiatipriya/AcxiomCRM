using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCrm.ViewModels.FollowUps;

public class FutureOrTodayDateAttribute : ValidationAttribute, IClientModelValidator
{
    public FutureOrTodayDateAttribute()
        : base("Follow-up date cannot be earlier than today.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.Date >= DateTime.Today;
        }
        return true;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (!context.Attributes.ContainsKey("data-val"))
        {
            context.Attributes.Add("data-val", "true");
        }
        if (!context.Attributes.ContainsKey("data-val-futureortoday"))
        {
            context.Attributes.Add("data-val-futureortoday", ErrorMessageString);
        }
    }
}

public class FollowUpFormViewModel
{
    public int FollowUpId { get; set; }

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Follow-up date is required.")]
    [DataType(DataType.DateTime)]
    [FutureOrTodayDate(ErrorMessage = "Follow-up date cannot be earlier than today.")]
    [Display(Name = "Follow-Up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today.AddHours(10);

    [Required(ErrorMessage = "Follow-up type is required.")]
    [StringLength(50)]
    [Display(Name = "Follow-Up Type")]
    public string FollowUpType { get; set; } = "Call";

    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
    [Display(Name = "Remarks / Discussion Notes")]
    public string? Remarks { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned";

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
}
