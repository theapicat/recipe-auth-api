using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Nåværende passord er påkrevd.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nytt passord er påkrevd.")]
    [MinLength(8, ErrorMessage = "Passordet må være minst 8 tegn.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).*$",
        ErrorMessage = "Passordet må inneholde minst én stor bokstav, én liten bokstav, ett tall og ett spesialtegn.")]
    public string NewPassword { get; set; } = string.Empty;
}