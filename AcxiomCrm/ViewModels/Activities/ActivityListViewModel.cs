using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.Activities;

public class ActivityListViewModel
{
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }

    public int TotalActivities { get; set; }
    public int CallsCount { get; set; }
    public int MeetingsCount { get; set; }
    public int EmailsCount { get; set; }
    public int TasksCount { get; set; }

    public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
}
