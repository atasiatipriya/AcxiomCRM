using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public DateTime FollowUpDate { get; set; }

    [Required]
    [StringLength(50)]
    public string FollowUpType { get; set; } = "Call";

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned";

    [StringLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
