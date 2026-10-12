using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.FollowUps;

public class FollowUpListViewModel
{
    public string? QuickFilter { get; set; } = "all";
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }

    public int TodayCount { get; set; }
    public int UpcomingCount { get; set; }
    public int OverdueCount { get; set; }

    public IEnumerable<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
