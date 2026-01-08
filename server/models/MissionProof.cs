using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class MissionProof
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

    [BsonElement("proofType")]
    public string ProofType { get; set; } = "Photo"; // Photo, Receipt, Signature

    [BsonElement("fileUrl")]
    public string? FileUrl { get; set; }

    [BsonElement("fileName")]
    public string? FileName { get; set; }

    [BsonElement("verificationCode")]
    public string? VerificationCode { get; set; } // 4-digit code

    [BsonElement("isVerified")]
    public bool IsVerified { get; set; } = false;

    [BsonElement("verifiedAt")]
    public DateTime? VerifiedAt { get; set; }

    [BsonElement("verifiedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? VerifiedBy { get; set; } // Patient ID

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}





