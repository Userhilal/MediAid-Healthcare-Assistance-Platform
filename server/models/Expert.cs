using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Expert
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("specialization")]
    public string? Specialization { get; set; }

    [BsonElement("licenseNumber")]
    public string? LicenseNumber { get; set; }

    [BsonElement("organization")]
    public string? Organization { get; set; }

    [BsonElement("validatedRequests")]
    public int ValidatedRequests { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


