using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.DTOs.Admin;

public class AddBlacklistRequest
{
    [Required(ErrorMessage = "E-post eller domene må oppgis.")]
    public string Pattern { get; set; } = string.Empty;

    [Required(ErrorMessage = "Må angi om det er eksakt e-post eller et domene.")]
    public BlacklistType Type { get; set; }

    public string? Reason { get; set; }
}