using MediAid.Models;

namespace MediAid.Services;

public interface IMessageService
{
    Task<Message?> CreateMessageAsync(string requestId, string senderId, string receiverId, string content, List<MessageAttachment>? attachments = null);
    Task<List<Message>> GetMessagesByRequestIdAsync(string requestId);
    Task<bool> MarkMessageAsReadAsync(string messageId, string userId);
    Task<bool> MarkMessageAsDeliveredAsync(string messageId, string userId);
    Task<int> GetUnreadMessageCountAsync(string userId, string requestId);
    Task<List<Message>> GetConversationsByUserIdAsync(string userId);
    Task<int> MarkAllMessagesAsReadAsync(string requestId, string userId);
}

