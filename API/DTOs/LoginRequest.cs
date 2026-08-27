using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "E-post er påkrevd.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passord er påkrevd.")]
    public string Password { get; set; } = string.Empty;
}