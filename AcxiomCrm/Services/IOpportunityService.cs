using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Opportunities;

namespace AcxiomCrm.Services;

public interface IOpportunityService
{
    Task<OpportunityListViewModel> GetOpportunitiesAsync(string? search = null, string? stage = null, string? status = null);
    Task<Opportunity?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(OpportunityFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(OpportunityFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateStageAsync(int id, string newStage);
    Task<bool> CanUserAccessOpportunityAsync(int id);
}
