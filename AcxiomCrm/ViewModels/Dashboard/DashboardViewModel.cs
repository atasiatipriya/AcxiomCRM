using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.Dashboard;

public class DashboardViewModel
{
    public string DateFilter { get; set; } = "All";

    // Primary KPI Metrics
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int ConvertedLeads { get; set; }

    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }

    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    public int PendingFollowUps { get; set; }
    public int CompletedFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }

    // Chart.js Datasets (JSON serializable)
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> PipelineStageLabels { get; set; } = new();
    public List<decimal> PipelineStageAmounts { get; set; } = new();
    public List<int> PipelineStageCounts { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesAmounts { get; set; } = new();

    // Recent CRM Activity Lists
    public IEnumerable<Lead> RecentLeads { get; set; } = new List<Lead>();
    public IEnumerable<Opportunity> RecentOpportunities { get; set; } = new List<Opportunity>();
    public IEnumerable<FollowUp> UpcomingFollowUps { get; set; } = new List<FollowUp>();
}
