using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Customers;

namespace AcxiomCrm.Services;

public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetCustomersAsync(string? search = null, string? status = null);
    Task<Customer?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(CustomerFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(CustomerFormViewModel model);
    Task<(bool Success, string? ErrorMessage)> SoftDeleteAsync(int id);
    Task<bool> CanUserAccessCustomerAsync(int customerId);
}
