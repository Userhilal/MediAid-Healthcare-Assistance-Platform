using MongoDB.Bson.Serialization.Attributes;

namespace MediAid.Models;

public class MessageAttachment
{
    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileUrl")]
    public string FileUrl { get; set; } = string.Empty;

    [BsonElement("fileType")]
    public string FileType { get; set; } = string.Empty; // image, document, etc.

    [BsonElement("fileSize")]
    public long FileSize { get; set; } // in bytes

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

