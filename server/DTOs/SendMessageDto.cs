using System.ComponentModel.DataAnnotations;

namespace MediAid.DTOs;

public class SendMessageDto
{
    [Required]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public List<string> AttachmentUrls { get; set; } = new();
}


