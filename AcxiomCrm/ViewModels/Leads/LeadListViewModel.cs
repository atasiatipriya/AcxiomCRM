using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.Leads;

public class LeadListViewModel
{
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? SourceFilter { get; set; }
    public IEnumerable<Lead> Leads { get; set; } = new List<Lead>();
}
