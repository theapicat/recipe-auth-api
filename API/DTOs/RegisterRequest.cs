using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "E-post er påkrevd.")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passord er påkrevd.")]
    [MinLength(8, ErrorMessage = "Passordet må være minst 8 tegn.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).*$",
        ErrorMessage = "Passordet må inneholde minst én stor bokstav, én liten bokstav, ett tall og ett spesialtegn.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fornavn er påkrevd.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Etternavn er påkrevd.")]
    public string LastName { get; set; } = string.Empty;
}