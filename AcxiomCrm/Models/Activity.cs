using System.ComponentModel.DataAnnotations;

namespace AcxiomCrm.Models;

public class Activity
{
    public int ActivityId { get; set; }

    [Required]
    [StringLength(50)]
    public string ActivityType { get; set; } = "Call";

    [Required]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime ActivityDate { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [StringLength(100)]
    public string? AssignedTo { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Completed";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
