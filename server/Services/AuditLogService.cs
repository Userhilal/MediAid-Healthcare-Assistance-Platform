using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class AuditLogService : IAuditLogService
{
    private readonly MongoDbContext _context;

    public AuditLogService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string action, string? entityType, string? entityId, string? ipAddress, string? userAgent, Dictionary<string, object>? details)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.InsertOneAsync(log);
    }

    public async Task<List<AuditLog>> GetLogsByUserIdAsync(string userId, int limit = 100)
    {
        return await _context.AuditLogs.Find(a => a.UserId == userId)
            .SortByDescending(a => a.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetLogsByActionAsync(string action, int limit = 100)
    {
        return await _context.AuditLogs.Find(a => a.Action == action)
            .SortByDescending(a => a.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }
}


