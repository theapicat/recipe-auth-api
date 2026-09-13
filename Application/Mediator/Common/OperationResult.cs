using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Common;

/// <summary>
/// Delt basisklasse for MediatR command/query-resultater i Application-laget.
/// Samler feltene som ellers gjentas identisk i hvert enkelt resultat (suksessflagg,
/// feilmelding og Identity-valideringsfeil), slik at hver feature kun trenger å
/// definere sine egne situasjonsspesifikke flagg og eventuell payload.
/// </summary>
public abstract class OperationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }
}
