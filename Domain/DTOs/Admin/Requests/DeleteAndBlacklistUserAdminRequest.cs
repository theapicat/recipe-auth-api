using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Admin.Requests;

public class DeleteAndBlacklistUserAdminRequest
{
    [Required(ErrorMessage = "Bruker-ID må oppgis.")]
    public string UserId { get; set; } = string.Empty;

    public string? Reason { get; set; }
}