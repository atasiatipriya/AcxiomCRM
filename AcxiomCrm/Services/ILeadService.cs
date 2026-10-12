using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Leads;

namespace AcxiomCrm.Services;

public interface ILeadService
{
    Task<IEnumerable<Lead>> GetLeadsAsync(string? search = null, string? status = null, string? source = null);
    Task<Lead?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(LeadFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(LeadFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int leadId, string newStatus);
    Task<(bool Success, string? ErrorMessage, int? CustomerId)> ConvertLeadAsync(LeadConvertViewModel model);
    Task<bool> CanUserAccessLeadAsync(int leadId);
    bool IsValidStatusTransition(string currentStatus, string newStatus);
}
