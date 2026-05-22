using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UserId { get; set; }

    [Required]
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // Login, CreateRequest, UpdateRequest, DeleteUser, etc.

    [BsonElement("entityType")]
    public string? EntityType { get; set; }

    [BsonElement("entityId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? EntityId { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("details")]
    public Dictionary<string, object>? Details { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



