using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MediAid.Models;

public class RequestStatus
{
    [BsonElement("status")]
    public string Status { get; set; } = "Open"; // Open, Assigned, InProgress, PendingVerification, Completed, Cancelled

    [BsonElement("changedAt")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("changedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ChangedBy { get; set; } // UserId

    [BsonElement("notes")]
    public string? Notes { get; set; }
}



