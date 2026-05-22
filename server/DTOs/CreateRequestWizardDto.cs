using System.ComponentModel.DataAnnotations;

namespace MediAid.DTOs;

// Étape 1: Type d'aide
public class RequestTypeDto
{
    [Required]
    public string Category { get; set; } = string.Empty; // Transport, Medication, Accompaniment, LightAssistance, PostHospitalization
}

// Étape 2: Détails
public class RequestDetailsDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public List<string> Documents { get; set; } = new(); // URLs des fichiers
}

// Étape 3: Urgence
public class RequestUrgencyDto
{
    [Required]
    public string Urgency { get; set; } = "Normal"; // Low, Normal, High, Critical
}

// Étape 4: Localisation
public class RequestLocationDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}

// Étape 5: Confirmation
public class RequestConfirmationDto
{
    public bool RequiresExpertValidation { get; set; } = false;
    public DateTime? RequestedDate { get; set; }
}

// DTO complet pour le wizard
public class CreateRequestWizardDto
{
    // Étape 1
    [Required]
    public string Category { get; set; } = string.Empty;

    // Étape 2
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public List<string> Documents { get; set; } = new();

    // Étape 3
    [Required]
    public string Urgency { get; set; } = "Normal";

    // Étape 4
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // Étape 5
    public bool RequiresExpertValidation { get; set; } = false;
    public DateTime? RequestedDate { get; set; }
}


