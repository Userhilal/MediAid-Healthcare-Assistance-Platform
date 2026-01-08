using System.ComponentModel.DataAnnotations;

namespace MediAid.DTOs;

public class PatientProfileDto
{
    public string? ProfilePhoto { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double LocationBlurRadius { get; set; } = 0.5;
    public string ContactPreference { get; set; } = "Chat";
    public bool AnonymousMode { get; set; } = false;
    public EmergencyContactDto? EmergencyContact { get; set; }
    public List<string> MedicalConditions { get; set; } = new();
}

public class EmergencyContactDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;
}

