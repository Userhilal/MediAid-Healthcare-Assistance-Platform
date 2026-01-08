using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty; // NewProposal, RequestAccepted, Message, RequestStatusChanged, etc.

    [Required]
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("relatedEntityId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RelatedEntityId { get; set; } // RequestId, ProposalId, etc.

    [BsonElement("relatedEntityType")]
    public string? RelatedEntityType { get; set; }

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


