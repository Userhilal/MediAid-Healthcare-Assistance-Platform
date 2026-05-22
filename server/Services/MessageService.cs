using MediAid.Data;
using MediAid.Models;
using MongoDB.Driver;

namespace MediAid.Services;

public class MessageService : IMessageService
{
    private readonly MongoDbContext _context;

    public MessageService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> CreateMessageAsync(string requestId, string senderId, string receiverId, string content, List<MessageAttachment>? attachments = null)
    {
        var message = new Message
        {
            RequestId = requestId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            Attachments = attachments ?? new List<MessageAttachment>(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Messages.InsertOneAsync(message);
        return message;
    }

    public async Task<List<Message>> GetMessagesByRequestIdAsync(string requestId)
    {
        return await _context.Messages
            .Find(m => m.RequestId == requestId)
            .SortBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkMessageAsReadAsync(string messageId, string userId)
    {
        var message = await _context.Messages.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        if (message == null || message.ReceiverId != userId)
        {
            return false;
        }

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        message.MessageStatus = "read";
        message.UpdatedAt = DateTime.UtcNow;

        var result = await _context.Messages.ReplaceOneAsync(m => m.Id == messageId, message);
        return result.ModifiedCount > 0;
    }
    
    public async Task<bool> MarkMessageAsDeliveredAsync(string messageId, string userId)
    {
        var message = await _context.Messages.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        if (message == null || message.ReceiverId != userId)
        {
            return false;
        }

        if (!message.IsDelivered)
        {
            message.IsDelivered = true;
            message.DeliveredAt = DateTime.UtcNow;
            message.MessageStatus = "delivered";
            message.UpdatedAt = DateTime.UtcNow;

            var result = await _context.Messages.ReplaceOneAsync(m => m.Id == messageId, message);
            return result.ModifiedCount > 0;
        }
        return true;
    }
    
    public async Task<List<Message>> GetConversationsByUserIdAsync(string userId)
    {
        // Get all unique request IDs where user is sender or receiver
        var messages = await _context.Messages
            .Find(m => m.SenderId == userId || m.ReceiverId == userId)
            .SortByDescending(m => m.CreatedAt)
            .ToListAsync();
            
        // Group by requestId and get latest message for each
        var conversations = messages
            .GroupBy(m => m.RequestId)
            .Select(g => g.First())
            .OrderByDescending(m => m.CreatedAt)
            .ToList();
            
        return conversations;
    }

    public async Task<int> GetUnreadMessageCountAsync(string userId, string requestId)
    {
        var count = await _context.Messages.CountDocumentsAsync(m => 
            m.RequestId == requestId && 
            m.ReceiverId == userId && 
            m.IsRead == false);
        return (int)count;
    }

    public async Task<int> MarkAllMessagesAsReadAsync(string requestId, string userId)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.RequestId, requestId),
            Builders<Message>.Filter.Eq(m => m.ReceiverId, userId),
            Builders<Message>.Filter.Eq(m => m.IsRead, false)
        );

        var update = Builders<Message>.Update
            .Set(m => m.IsRead, true)
            .Set(m => m.ReadAt, DateTime.UtcNow)
            .Set(m => m.MessageStatus, "read")
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        var result = await _context.Messages.UpdateManyAsync(filter, update);
        return (int)result.ModifiedCount;
    }
}

