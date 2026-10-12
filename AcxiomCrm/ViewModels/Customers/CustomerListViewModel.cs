using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.Customers;

public class CustomerListViewModel
{
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public IEnumerable<Customer> Customers { get; set; } = new List<Customer>();
}
