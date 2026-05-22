using MediAid.Models;

namespace MediAid.Services;

public interface IAuditLogService
{
    Task LogAsync(string? userId, string action, string? entityType, string? entityId, string? ipAddress, string? userAgent, Dictionary<string, object>? details);
    Task<List<AuditLog>> GetLogsByUserIdAsync(string userId, int limit = 100);
    Task<List<AuditLog>> GetLogsByActionAsync(string action, int limit = 100);
}



