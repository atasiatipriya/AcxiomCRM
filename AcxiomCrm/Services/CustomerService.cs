using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Customers;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public CustomerService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync(string? search = null, string? status = null)
    {
        var query = _context.Customers.AsQueryable();

        if (_currentUser.IsSalesExecutive)
        {
            var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
            query = query.Where(c => c.CreatedBy == userIdentifier || c.CreatedBy == _currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return null;

        if (!await CanUserAccessCustomerAsync(id))
        {
            return null;
        }

        return customer;
    }

    public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(CustomerFormViewModel model)
    {
        var emailExists = await _context.Customers.AnyAsync(c => c.Email == model.Email);
        if (emailExists)
        {
            return (false, "A customer with this email address already exists.", null);
        }

        var phoneExists = await _context.Customers.AnyAsync(c => c.Phone == model.Phone);
        if (phoneExists)
        {
            return (false, "A customer with this phone number already exists.", null);
        }

        var code = model.CustomerCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            var totalCount = await _context.Customers.CountAsync();
            code = $"CUST-{1001 + totalCount}";
        }
        else
        {
            var codeExists = await _context.Customers.AnyAsync(c => c.CustomerCode == code);
            if (codeExists)
            {
                return (false, "Customer code already exists. Please choose a unique code.", null);
            }
        }

        var creator = _currentUser.UserName ?? _currentUser.UserId ?? "System";

        var customer = new Customer
        {
            CustomerCode = code,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Address = model.Address?.Trim(),
            City = model.City?.Trim(),
            State = model.State?.Trim(),
            Status = string.IsNullOrWhiteSpace(model.Status) ? "Active" : model.Status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = creator
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"Created customer {customer.CustomerName} ({customer.CustomerCode})",
            _currentUser.IpAddress);

        return (true, null, customer);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(CustomerFormViewModel model)
    {
        if (!await CanUserAccessCustomerAsync(model.CustomerId))
        {
            return (false, "You do not have permission to modify this customer.");
        }

        var customer = await _context.Customers.FindAsync(model.CustomerId);
        if (customer == null)
        {
            return (false, "Customer not found.");
        }

        var emailExists = await _context.Customers.AnyAsync(c => c.Email == model.Email && c.CustomerId != model.CustomerId);
        if (emailExists)
        {
            return (false, "A customer with this email address already exists.");
        }

        var phoneExists = await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.CustomerId != model.CustomerId);
        if (phoneExists)
        {
            return (false, "A customer with this phone number already exists.");
        }

        var oldValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        customer.CustomerName = model.CustomerName.Trim();
        customer.Email = model.Email.Trim().ToLower();
        customer.Phone = model.Phone.Trim();
        customer.CompanyName = model.CompanyName?.Trim();
        customer.Address = model.Address?.Trim();
        customer.City = model.City?.Trim();
        customer.State = model.State?.Trim();
        customer.Status = model.Status;

        await _context.SaveChangesAsync();

        var newValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Update",
            "Customer",
            customer.CustomerId.ToString(),
            oldValue,
            newValue,
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> SoftDeleteAsync(int id)
    {
        if (!await CanUserAccessCustomerAsync(id))
        {
            return (false, "You do not have permission to deactivate this customer.");
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return (false, "Customer not found.");
        }

        var previousStatus = customer.Status;
        customer.Status = "Inactive";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Delete",
            "Customer",
            customer.CustomerId.ToString(),
            $"Status: {previousStatus}",
            "Status: Inactive (Soft-deleted)",
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<bool> CanUserAccessCustomerAsync(int customerId)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return true;
        }

        var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer == null) return false;

        var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
        return customer.CreatedBy == userIdentifier || customer.CreatedBy == _currentUser.UserId;
    }
}
