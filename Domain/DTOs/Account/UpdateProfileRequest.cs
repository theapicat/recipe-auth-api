using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Fornavn er påkrevd.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Etternavn er påkrevd.")]
    public string LastName { get; set; } = string.Empty;
}