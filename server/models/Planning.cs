using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Planning
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("aidantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AidantId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("timeSlots")]
    public List<PlanningTimeSlot> TimeSlots { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlanningTimeSlot
{
    [BsonElement("startTime")]
    public TimeSpan StartTime { get; set; }

    [BsonElement("endTime")]
    public TimeSpan EndTime { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "Available"; // Available, Mission, Blocked, Unavailable

    [BsonElement("requestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RequestId { get; set; } // Si type = Mission

    [BsonElement("title")]
    public string? Title { get; set; } // Titre de la mission ou note

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("isRecurring")]
    public bool IsRecurring { get; set; } = false;

    [BsonElement("recurringPattern")]
    public string? RecurringPattern { get; set; } // Daily, Weekly, Monthly
}





