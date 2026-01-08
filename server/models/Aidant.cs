using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Aidant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("profilePhoto")]
    public string? ProfilePhoto { get; set; }

    [BsonElement("location")]
    public Location? Location { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("interventionRadius")]
    public double InterventionRadius { get; set; } = 10.0; // in kilometers

    [BsonElement("languages")]
    public List<string> Languages { get; set; } = new(); // Langues parlées

    [BsonElement("skills")]
    public List<string> Skills { get; set; } = new(); // Types d'aide proposés

    [BsonElement("availabilitySchedule")]
    public AvailabilitySchedule? AvailabilitySchedule { get; set; }

    [BsonElement("unavailableDates")]
    public List<DateTime> UnavailableDates { get; set; } = new(); // Dates exceptionnelles d'indisponibilité

    [BsonElement("certifications")]
    public List<Certification> Certifications { get; set; } = new();

    [BsonElement("totalHours")]
    public double TotalHours { get; set; } = 0.0; // Heures de bénévolat cumulées

    [BsonElement("isVerified")]
    public bool IsVerified { get; set; } = false;

    [BsonElement("reputationScore")]
    public double ReputationScore { get; set; } = 0.0;

    [BsonElement("totalMissions")]
    public int TotalMissions { get; set; } = 0;

    [BsonElement("completedMissions")]
    public int CompletedMissions { get; set; } = 0;

    [BsonElement("badges")]
    public List<string> Badges { get; set; } = new();

    [BsonElement("availabilityStatus")]
    public string AvailabilityStatus { get; set; } = "Available"; // Available, Busy, Unavailable

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Location
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } = new double[2]; // [longitude, latitude]
}

public class AvailabilitySchedule
{
    [BsonElement("monday")]
    public DayAvailability? Monday { get; set; }
    
    [BsonElement("tuesday")]
    public DayAvailability? Tuesday { get; set; }
    
    [BsonElement("wednesday")]
    public DayAvailability? Wednesday { get; set; }
    
    [BsonElement("thursday")]
    public DayAvailability? Thursday { get; set; }
    
    [BsonElement("friday")]
    public DayAvailability? Friday { get; set; }
    
    [BsonElement("saturday")]
    public DayAvailability? Saturday { get; set; }
    
    [BsonElement("sunday")]
    public DayAvailability? Sunday { get; set; }
}

public class DayAvailability
{
    [BsonElement("isAvailable")]
    public bool IsAvailable { get; set; } = false;

    [BsonElement("timeSlots")]
    public List<TimeSlot> TimeSlots { get; set; } = new();
}

public class TimeSlot
{
    [BsonElement("startTime")]
    public TimeSpan StartTime { get; set; }

    [BsonElement("endTime")]
    public TimeSpan EndTime { get; set; }
}

public class Certification
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("issuingOrganization")]
    public string? IssuingOrganization { get; set; }

    [BsonElement("issueDate")]
    public DateTime? IssueDate { get; set; }

    [BsonElement("expiryDate")]
    public DateTime? ExpiryDate { get; set; }

    [BsonElement("documentUrl")]
    public string? DocumentUrl { get; set; } // URL du document de certification
}
