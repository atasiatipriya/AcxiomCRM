using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Leads;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public LeadService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        if (string.Equals(currentStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(currentStatus, "Converted", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return currentStatus switch
        {
            "New" => newStatus is "Contacted" or "Unqualified" or "Lost",
            "Contacted" => newStatus is "Qualified" or "Unqualified" or "Lost",
            "Qualified" => newStatus is "Converted" or "Lost" or "Contacted",
            "Unqualified" => newStatus is "Contacted" or "Lost",
            "Lost" => newStatus is "Contacted" or "New",
            _ => false
        };
    }

    public async Task<IEnumerable<Lead>> GetLeadsAsync(string? search = null, string? status = null, string? source = null)
    {
        var query = _context.Leads.AsQueryable();

        if (_currentUser.IsSalesExecutive)
        {
            var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
            query = query.Where(l => l.AssignedTo == userIdentifier || l.AssignedTo == _currentUser.UserId || l.AssignedTo == null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(term) ||
                l.Email.ToLower().Contains(term) ||
                l.Phone.Contains(term) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(l => l.Source == source);
        }

        return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<Lead?> GetByIdAsync(int id)
    {
        var lead = await _context.Leads
            .Include(l => l.Opportunities)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return null;

        if (!await CanUserAccessLeadAsync(id))
        {
            return null;
        }

        return lead;
    }

    public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(LeadFormViewModel model)
    {
        var code = model.LeadCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            var totalCount = await _context.Leads.CountAsync();
            code = $"LEAD-{2001 + totalCount}";
        }
        else
        {
            var codeExists = await _context.Leads.AnyAsync(l => l.LeadCode == code);
            if (codeExists)
            {
                return (false, "Lead code already exists. Please choose a unique code.", null);
            }
        }

        var assigned = model.AssignedTo;
        if (string.IsNullOrWhiteSpace(assigned))
        {
            assigned = _currentUser.UserName ?? _currentUser.UserId;
        }

        var lead = new Lead
        {
            LeadCode = code,
            LeadName = model.LeadName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Source = model.Source?.Trim() ?? "Website",
            Status = string.IsNullOrWhiteSpace(model.Status) ? "New" : model.Status,
            ExpectedValue = model.ExpectedValue,
            AssignedTo = assigned,
            CreatedDate = DateTime.UtcNow
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"Created lead {lead.LeadName} ({lead.LeadCode}) with value {lead.ExpectedValue:C}",
            _currentUser.IpAddress);

        return (true, null, lead);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(LeadFormViewModel model)
    {
        if (!await CanUserAccessLeadAsync(model.LeadId))
        {
            return (false, "You do not have permission to modify this lead.");
        }

        var lead = await _context.Leads.FindAsync(model.LeadId);
        if (lead == null)
        {
            return (false, "Lead not found.");
        }

        if (lead.Status != model.Status && !IsValidStatusTransition(lead.Status, model.Status))
        {
            return (false, $"Invalid lead status transition from {lead.Status} to {model.Status}.");
        }

        var oldValue = $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}";

        lead.LeadName = model.LeadName.Trim();
        lead.Email = model.Email.Trim().ToLower();
        lead.Phone = model.Phone.Trim();
        lead.CompanyName = model.CompanyName?.Trim();
        lead.Source = model.Source;
        lead.Status = model.Status;
        lead.ExpectedValue = model.ExpectedValue;

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            lead.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValue = $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}";

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Update",
            "Lead",
            lead.LeadId.ToString(),
            oldValue,
            newValue,
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int leadId, string newStatus)
    {
        if (!await CanUserAccessLeadAsync(leadId))
        {
            return (false, "You do not have permission to update this lead.");
        }

        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return (false, "Lead not found.");
        }

        if (!IsValidStatusTransition(lead.Status, newStatus))
        {
            return (false, $"Invalid lead status transition from {lead.Status} to {newStatus}.");
        }

        var previous = lead.Status;
        lead.Status = newStatus;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "UpdateStatus",
            "Lead",
            lead.LeadId.ToString(),
            $"Status: {previous}",
            $"Status: {newStatus}",
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage, int? CustomerId)> ConvertLeadAsync(LeadConvertViewModel model)
    {
        if (!await CanUserAccessLeadAsync(model.LeadId))
        {
            return (false, "You do not have permission to convert this lead.", null);
        }

        var lead = await _context.Leads.FindAsync(model.LeadId);
        if (lead == null)
        {
            return (false, "Lead not found.", null);
        }

        if (string.Equals(lead.Status, "Converted", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Lead has already been converted.", null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check or Create Customer
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email || c.Phone == model.Phone);
            Customer customer;

            if (existingCustomer != null)
            {
                customer = existingCustomer;
            }
            else
            {
                var totalCustomers = await _context.Customers.CountAsync();
                customer = new Customer
                {
                    CustomerCode = $"CUST-{1001 + totalCustomers}",
                    CustomerName = model.CustomerName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Phone = model.Phone.Trim(),
                    CompanyName = model.CompanyName?.Trim(),
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserName ?? _currentUser.UserId ?? "System"
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // 2. Optionally create Opportunity
            if (model.CreateOpportunity)
            {
                if (model.OpportunityAmount <= 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "Opportunity Amount must be greater than 0.", null);
                }

                if (model.ExpectedCloseDate.HasValue && model.ExpectedCloseDate.Value.Date < DateTime.Today)
                {
                    await transaction.RollbackAsync();
                    return (false, "Expected Close Date cannot be in the past.", null);
                }

                var opportunityName = string.IsNullOrWhiteSpace(model.OpportunityName)
                    ? $"{customer.CustomerName} - Deal"
                    : model.OpportunityName.Trim();

                var opportunity = new Opportunity
                {
                    OpportunityName = opportunityName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount,
                    Stage = "Qualification",
                    Probability = 25,
                    ExpectedCloseDate = model.ExpectedCloseDate ?? DateTime.Today.AddMonths(1),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = lead.AssignedTo ?? _currentUser.UserName
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();
            }

            // 3. Mark Lead as Converted
            var prevStatus = lead.Status;
            lead.Status = "Converted";
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _auditService.LogAsync(
                _currentUser.UserId,
                "Convert",
                "Lead",
                lead.LeadId.ToString(),
                $"Status: {prevStatus}",
                $"Converted to Customer ID: {customer.CustomerId}",
                _currentUser.IpAddress);

            return (true, null, customer.CustomerId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lead conversion failed: {ex.Message}", null);
        }
    }

    public async Task<bool> CanUserAccessLeadAsync(int leadId)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return true;
        }

        var lead = await _context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == leadId);
        if (lead == null) return false;

        var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
        return lead.AssignedTo == userIdentifier || lead.AssignedTo == _currentUser.UserId || lead.AssignedTo == null;
    }
}
