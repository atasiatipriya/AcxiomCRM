using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.ViewModels.Opportunities;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Services;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public OpportunityService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<OpportunityListViewModel> GetOpportunitiesAsync(string? search = null, string? stage = null, string? status = null)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .AsQueryable();

        if (_currentUser.IsSalesExecutive)
        {
            var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
            query = query.Where(o => o.AssignedTo == userIdentifier || o.AssignedTo == _currentUser.UserId || o.AssignedTo == null);
        }

        var allList = await query.ToListAsync();

        var totalCount = allList.Count;
        var openCount = allList.Count(o => o.Status == "Open");
        var wonCount = allList.Count(o => o.Status == "Won");
        var lostCount = allList.Count(o => o.Status == "Lost");

        var openOpportunities = allList.Where(o => o.Status == "Open").ToList();
        var totalPipeline = openOpportunities.Sum(o => o.Amount);
        var weightedPipeline = openOpportunities.Sum(o => (o.Amount * o.Probability) / 100m);

        var filtered = allList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            filtered = filtered.Where(o =>
                o.OpportunityName.ToLower().Contains(term) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                (o.Lead != null && o.Lead.LeadName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            filtered = filtered.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(o => o.Status == status);
        }

        return new OpportunityListViewModel
        {
            SearchTerm = search,
            StageFilter = stage,
            StatusFilter = status,
            TotalCount = totalCount,
            OpenCount = openCount,
            WonCount = wonCount,
            LostCount = lostCount,
            TotalPipelineValue = totalPipeline,
            WeightedPipelineValue = weightedPipeline,
            Opportunities = filtered.OrderByDescending(o => o.CreatedDate).ToList()
        };
    }

    public async Task<Opportunity?> GetByIdAsync(int id)
    {
        var opp = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opp == null) return null;

        if (!await CanUserAccessOpportunityAsync(id))
        {
            return null;
        }

        return opp;
    }

    public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(OpportunityFormViewModel model)
    {
        if (model.Amount <= 0)
        {
            return (false, "Opportunity Amount must be greater than 0.", null);
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            return (false, "Probability must be between 0 and 100.", null);
        }

        if (model.Status != "Lost" && model.Status != "Won" && model.ExpectedCloseDate.Date < DateTime.Today)
        {
            return (false, "Expected Close Date cannot be in the past.", null);
        }

        var status = model.Stage switch
        {
            "Won" => "Won",
            "Lost" => "Lost",
            _ => "Open"
        };

        var assigned = model.AssignedTo;
        if (string.IsNullOrWhiteSpace(assigned))
        {
            assigned = _currentUser.UserName ?? _currentUser.UserId;
        }

        var opp = new Opportunity
        {
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Stage == "Won" ? 100 : model.Stage == "Lost" ? 0 : model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = status,
            AssignedTo = assigned,
            CreatedDate = DateTime.UtcNow
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Create",
            "Opportunity",
            opp.OpportunityId.ToString(),
            null,
            $"Created opportunity {opp.OpportunityName} for {opp.Amount:C} ({opp.Stage})",
            _currentUser.IpAddress);

        return (true, null, opp);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(OpportunityFormViewModel model)
    {
        if (!await CanUserAccessOpportunityAsync(model.OpportunityId))
        {
            return (false, "You do not have permission to modify this opportunity.");
        }

        if (model.Amount <= 0)
        {
            return (false, "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            return (false, "Probability must be between 0 and 100.");
        }

        if (model.Status != "Lost" && model.Status != "Won" && model.ExpectedCloseDate.Date < DateTime.Today)
        {
            return (false, "Expected Close Date cannot be in the past.");
        }

        var opp = await _context.Opportunities.FindAsync(model.OpportunityId);
        if (opp == null)
        {
            return (false, "Opportunity not found.");
        }

        var oldValue = $"Amount: {opp.Amount:C}, Stage: {opp.Stage}, Status: {opp.Status}, Probability: {opp.Probability}%";

        var status = model.Stage switch
        {
            "Won" => "Won",
            "Lost" => "Lost",
            _ => "Open"
        };

        opp.OpportunityName = model.OpportunityName.Trim();
        opp.CustomerId = model.CustomerId;
        opp.LeadId = model.LeadId;
        opp.Amount = model.Amount;
        opp.Stage = model.Stage;
        opp.Probability = model.Stage == "Won" ? 100 : model.Stage == "Lost" ? 0 : model.Probability;
        opp.ExpectedCloseDate = model.ExpectedCloseDate;
        opp.Status = status;

        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            opp.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValue = $"Amount: {opp.Amount:C}, Stage: {opp.Stage}, Status: {opp.Status}, Probability: {opp.Probability}%";

        await _auditService.LogAsync(
            _currentUser.UserId,
            "Update",
            "Opportunity",
            opp.OpportunityId.ToString(),
            oldValue,
            newValue,
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateStageAsync(int id, string newStage)
    {
        if (!await CanUserAccessOpportunityAsync(id))
        {
            return (false, "You do not have permission to modify this opportunity.");
        }

        var opp = await _context.Opportunities.FindAsync(id);
        if (opp == null)
        {
            return (false, "Opportunity not found.");
        }

        var oldStage = opp.Stage;
        var oldStatus = opp.Status;

        opp.Stage = newStage;
        if (newStage == "Won")
        {
            opp.Status = "Won";
            opp.Probability = 100;
        }
        else if (newStage == "Lost")
        {
            opp.Status = "Lost";
            opp.Probability = 0;
        }
        else
        {
            opp.Status = "Open";
            opp.Probability = newStage switch
            {
                "Qualification" => 25,
                "Proposal" => 50,
                "Negotiation" => 75,
                _ => opp.Probability
            };
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            _currentUser.UserId,
            "UpdateStage",
            "Opportunity",
            opp.OpportunityId.ToString(),
            $"Stage: {oldStage} ({oldStatus})",
            $"Stage: {newStage} ({opp.Status})",
            _currentUser.IpAddress);

        return (true, null);
    }

    public async Task<bool> CanUserAccessOpportunityAsync(int id)
    {
        if (_currentUser.IsAdmin || _currentUser.IsManager)
        {
            return true;
        }

        var opp = await _context.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (opp == null) return false;

        var userIdentifier = _currentUser.UserName ?? _currentUser.UserId ?? "";
        return opp.AssignedTo == userIdentifier || opp.AssignedTo == _currentUser.UserId || opp.AssignedTo == null;
    }
}
