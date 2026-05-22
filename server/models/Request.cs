using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Request
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("patientId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // Transport, Medication, Accompaniment, LightAssistance, PostHospitalization

    [BsonElement("urgency")]
    public string Urgency { get; set; } = "Normal"; // Low, Normal, High, Critical

    [BsonElement("requestedDate")]
    public DateTime? RequestedDate { get; set; }

    [BsonElement("location")]
    public Location? Location { get; set; }

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("postalCode")]
    public string? PostalCode { get; set; }

    [BsonElement("documents")]
    public List<string> Documents { get; set; } = new(); // URLs or file paths

    [BsonElement("status")]
    public string Status { get; set; } = "Open"; // Open, Assigned, InProgress, PendingVerification, Completed, Cancelled

    [BsonElement("assignedAidantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedAidantId { get; set; }

    [BsonElement("expertId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ExpertId { get; set; }

    [BsonElement("expertRecommendations")]
    public string? ExpertRecommendations { get; set; }

    [BsonElement("requiresExpertValidation")]
    public bool RequiresExpertValidation { get; set; } = false;

    [BsonElement("isExpertValidated")]
    public bool IsExpertValidated { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("verificationCode")]
    public string? VerificationCode { get; set; } // 4-digit code for patient verification

    [BsonElement("aidantLocation")]
    public Location? AidantLocation { get; set; } // Current GPS location of aidant

    [BsonElement("lastCheckInAt")]
    public DateTime? LastCheckInAt { get; set; }

    [BsonElement("isAidantOnSite")]
    public bool IsAidantOnSite { get; set; } = false; // Within 50m radius
}




