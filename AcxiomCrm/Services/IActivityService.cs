using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Activities;

namespace AcxiomCrm.Services;

public interface IActivityService
{
    Task<ActivityListViewModel> GetActivitiesAsync(string? search = null, string? type = null, string? status = null);
    Task<Activity?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateAsync(ActivityFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(ActivityFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> ToggleStatusAsync(int id);
    Task<bool> CanUserAccessActivityAsync(int id);
}
