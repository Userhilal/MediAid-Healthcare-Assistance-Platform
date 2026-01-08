using System.ComponentModel.DataAnnotations;

namespace MediAid.DTOs;

public class CreateRequestDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    public string Urgency { get; set; } = "Normal";

    public DateTime? RequestedDate { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    public List<string> Documents { get; set; } = new();

    public bool RequiresExpertValidation { get; set; } = false;
}


