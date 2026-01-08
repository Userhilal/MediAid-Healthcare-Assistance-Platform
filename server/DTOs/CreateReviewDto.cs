using System.ComponentModel.DataAnnotations;

namespace MediAid.DTOs;

public class CreateReviewDto
{
    [Required]
    [Display(Name = "Note")]
    [Range(1, 5, ErrorMessage = "La note doit être entre 1 et 5 étoiles.")]
    public int Rating { get; set; }

    [Display(Name = "Commentaire")]
    [MaxLength(1000, ErrorMessage = "Le commentaire ne peut pas dépasser 1000 caractères.")]
    public string? Comment { get; set; }

    [Required]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public string AidantId { get; set; } = string.Empty;
}

