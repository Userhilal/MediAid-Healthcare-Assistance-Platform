using MediAid.Models;

namespace MediAid.Services;

public interface INotificationService
{
    Task<Notification?> CreateNotificationAsync(string userId, string type, string title, string message, string? relatedEntityId = null, string? relatedEntityType = null);
    Task<List<Notification>> GetNotificationsByUserIdAsync(string userId, bool unreadOnly = false);
    Task<bool> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
}


