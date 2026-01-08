using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediAid.Models;

public class AidantComment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("targetAidantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TargetAidantId { get; set; } = string.Empty; // L'aidant dont on commente le profil

    [Required]
    [BsonElement("authorAidantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorAidantId { get; set; } = string.Empty; // L'aidant qui écrit le commentaire

    [Required]
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int? Rating { get; set; } // Note optionnelle de 1 à 5

    [BsonElement("isPublic")]
    public bool IsPublic { get; set; } = true; // Si le commentaire est visible par tous ou seulement par l'aidant cible

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}





