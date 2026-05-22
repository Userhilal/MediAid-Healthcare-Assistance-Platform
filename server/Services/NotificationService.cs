using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class NotificationService : INotificationService
{
    private readonly MongoDbContext _context;

    public NotificationService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> CreateNotificationAsync(string userId, string type, string title, string message, string? relatedEntityId = null, string? relatedEntityType = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Notifications.InsertOneAsync(notification);
        return notification;
    }

    public async Task<List<Notification>> GetNotificationsByUserIdAsync(string userId, bool unreadOnly = false)
    {
        var filterBuilder = Builders<Notification>.Filter;
        var filter = filterBuilder.Eq(n => n.UserId, userId);

        if (unreadOnly)
        {
            filter &= filterBuilder.Eq(n => n.IsRead, false);
        }

        return await _context.Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
    {
        var notification = await _context.Notifications.Find(n => n.Id == notificationId && n.UserId == userId).FirstOrDefaultAsync();
        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        var result = await _context.Notifications.ReplaceOneAsync(n => n.Id == notificationId, notification);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId) & 
                     Builders<Notification>.Filter.Eq(n => n.IsRead, false);
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        var result = await _context.Notifications.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}



