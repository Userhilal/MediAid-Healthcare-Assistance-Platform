using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class Review
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

    [Required]
    [BsonElement("patientId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [BsonElement("rating")]
    [BsonRepresentation(BsonType.Int32)]
    public int Rating { get; set; } // 1-5 stars

    [BsonElement("comment")]
    public string? Comment { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



