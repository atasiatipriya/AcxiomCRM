using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCrm.Models;

public class Lead
{
    public int LeadId { get; set; }

    [Required]
    [StringLength(50)]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CompanyName { get; set; }

    [StringLength(50)]
    public string? Source { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "New";

    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? AssignedTo { get; set; }

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
