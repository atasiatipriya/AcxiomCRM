using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCrm.Models;

public class Opportunity
{
    public int OpportunityId { get; set; }

    [Required]
    [StringLength(100)]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string Stage { get; set; } = "Qualification";

    public int Probability { get; set; }

    public DateTime ExpectedCloseDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? AssignedTo { get; set; }

    [NotMapped]
    public decimal WeightedValue => (Amount * Probability) / 100m;
}
