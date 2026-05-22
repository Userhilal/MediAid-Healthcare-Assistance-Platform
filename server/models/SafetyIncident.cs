using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class SafetyIncident
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("requestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    [BsonElement("reportedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReportedBy { get; set; } = string.Empty; // User ID

    [BsonElement("incidentType")]
    public string IncidentType { get; set; } = "General"; // Emergency, Medical, Safety, Other

    [BsonElement("severity")]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("location")]
    public Location? Location { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Reported"; // Reported, InReview, Resolved, Closed

    [BsonElement("adminNotes")]
    public string? AdminNotes { get; set; }

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}






