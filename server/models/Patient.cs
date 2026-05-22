using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Patient
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("profilePhoto")]
    public string? ProfilePhoto { get; set; } // URL ou chemin du fichier

    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("postalCode")]
    public string? PostalCode { get; set; }

    [BsonElement("location")]
    public Location? Location { get; set; } // Position approximative

    [BsonElement("locationBlurRadius")]
    public double LocationBlurRadius { get; set; } = 0.5; // Rayon de floutage en km

    [BsonElement("contactPreference")]
    public string ContactPreference { get; set; } = "Chat"; // Chat, Phone, Email

    [BsonElement("anonymousMode")]
    public bool AnonymousMode { get; set; } = false; // Mode anonyme partiel

    [BsonElement("emergencyContact")]
    public EmergencyContact? EmergencyContact { get; set; }

    [BsonElement("trustedContacts")]
    public List<EmergencyContact> TrustedContacts { get; set; } = new();

    [BsonElement("medicalConditions")]
    public List<string> MedicalConditions { get; set; } = new();

    [BsonElement("blockedAidantIds")]
    public List<string> BlockedAidantIds { get; set; } = new(); // Liste des IDs d'aidants bloqués

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EmergencyContact
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("relationship")]
    public string Relationship { get; set; } = string.Empty;
}



