using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("requestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    [BsonElement("senderId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    [BsonElement("receiverId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("attachments")]
    public List<MessageAttachment> Attachments { get; set; } = new();

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }
    
    [BsonElement("isDelivered")]
    public bool IsDelivered { get; set; } = false;
    
    [BsonElement("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }
    
    [BsonElement("messageStatus")]
    public string MessageStatus { get; set; } = "sent"; // sent, delivered, read

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isEphemeral")]
    public bool IsEphemeral { get; set; } = false; // Images that auto-delete after mission completion

    [BsonElement("deleteAfter")]
    public DateTime? DeleteAfter { get; set; } // When to delete ephemeral content
}


