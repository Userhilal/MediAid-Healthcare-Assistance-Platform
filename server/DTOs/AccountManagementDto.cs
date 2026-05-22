using System.ComponentModel.DataAnnotations;

namespace MediAid.DTOs;

public class ChangePasswordDto
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Ancien mot de passe")]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    [Display(Name = "Nouveau mot de passe")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    [Display(Name = "Confirmer le nouveau mot de passe")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangeEmailDto
{
    [Required]
    [EmailAddress]
    [Display(Name = "Nouvel email")]
    public string NewEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    public string CurrentEmail { get; set; } = string.Empty;
}

public class DeleteAccountDto
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Confirmer la suppression")]
    public bool ConfirmDeletion { get; set; } = false;
}


