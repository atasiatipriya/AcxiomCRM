using AcxiomCrm.Data;
using AcxiomCrm.Models;

namespace AcxiomCrm.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string? userId,
        string action,
        string entityName,
        string? recordId = null,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null)
    {
        var log = new AuditLog
        {
            UserId = string.IsNullOrWhiteSpace(userId) ? "Anonymous" : userId,
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedDate = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
