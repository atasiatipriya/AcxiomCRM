using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.FollowUps;

namespace AcxiomCrm.Services;

public interface IFollowUpService
{
    Task<FollowUpListViewModel> GetFollowUpsAsync(string? quickFilter = null, string? status = null, string? type = null);
    Task<FollowUp?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(FollowUpFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(FollowUpFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? remarks);
    Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks);
    Task<bool> CanUserAccessFollowUpAsync(int id);
}
