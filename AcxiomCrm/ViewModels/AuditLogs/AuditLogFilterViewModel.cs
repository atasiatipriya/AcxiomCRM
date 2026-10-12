using AcxiomCrm.Models;

namespace AcxiomCrm.ViewModels.AuditLogs;

public class AuditLogFilterViewModel
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int TotalLogs { get; set; }
    public IEnumerable<AuditLog> Logs { get; set; } = new List<AuditLog>();
}
