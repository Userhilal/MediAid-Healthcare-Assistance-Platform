using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class MissionCheckIn
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

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("distanceFromDestination")]
    public double DistanceFromDestination { get; set; } // in meters

    [BsonElement("isWithinRadius")]
    public bool IsWithinRadius { get; set; } // Within 50m

    [BsonElement("checkInType")]
    public string CheckInType { get; set; } = "Arrival"; // Arrival, Departure, InProgress

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}






