namespace AcxiomCrm.ViewModels.Reports;

public class ReportsIndexViewModel
{
    public string ActiveReport { get; set; } = "Pipeline";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? StatusFilter { get; set; }

    // Summary Counts
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int TotalFollowUps { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedValue { get; set; }

    // Report Item Collections
    public List<CustomerReportRow> CustomerRows { get; set; } = new();
    public List<LeadReportRow> LeadRows { get; set; } = new();
    public List<OpportunityReportRow> OpportunityRows { get; set; } = new();
    public List<FollowUpReportRow> FollowUpRows { get; set; } = new();
}

public class CustomerReportRow
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
}

public class LeadReportRow
{
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string Status { get; set; } = "New";
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; }
}

public class OpportunityReportRow
{
    public string OpportunityName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedValue { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = "Open";
    public string? AssignedTo { get; set; }
}

public class FollowUpReportRow
{
    public DateTime FollowUpDate { get; set; }
    public string? Remarks { get; set; }
    public string FollowUpType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
}
