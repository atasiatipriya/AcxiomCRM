using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.Opportunities;

public class OpportunityListViewModel
{
    public string? SearchTerm { get; set; }
    public string? StageFilter { get; set; }
    public string? StatusFilter { get; set; }

    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public int WonCount { get; set; }
    public int LostCount { get; set; }

    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    public IEnumerable<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
