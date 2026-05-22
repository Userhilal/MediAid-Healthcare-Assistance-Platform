using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Proposal
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("requestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    [BsonElement("aidantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AidantId { get; set; } = string.Empty;

    [BsonElement("message")]
    public string? Message { get; set; }

    [BsonElement("estimatedArrivalTime")]
    public DateTime? EstimatedArrivalTime { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Cancelled

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("respondedAt")]
    public DateTime? RespondedAt { get; set; }
}



