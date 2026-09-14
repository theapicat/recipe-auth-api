using System.Security.Claims;
using Persistence.Context;

namespace Application.TokenService.Interfaces;

public interface ITokenService
{
    Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user);
    Task<string> GenerateAccessTokenAsync(ApplicationUser user);

    /// <summary>
    /// Utsteder et ekte access-/refresh-token-par via OpenIddicts egen token-genererings-pipeline
    /// (samme handler-kjede som password-/refresh-grant-flyten i AuthorizationController bruker),
    /// men uten å gå via en levende /connect/token-HTTP-forespørsel. Brukes av Google-callback-flyten,
    /// slik at refresh-tokenet blir et ekte, av OpenIddict lagret og senere revokerbart token —
    /// ikke en håndrullet streng.
    /// </summary>
    /// <param name="user">Brukeren tokens skal utstedes for.</param>
    /// <param name="baseUri">
    /// Skjema+host fra den faktiske innkommende HTTP-forespørselen (f.eks. https://localhost:7001).
    /// OpenIddicts interne pipeline krever en issuer den enten får fra Options.Issuer (ikke satt i
    /// dette prosjektet) eller — som her — fra transaksjonens BaseUri, akkurat slik den ellers ville
    /// blitt utledet automatisk fra en ekte /connect/token-forespørsel. Uten denne kaster OpenIddict
    /// "The issuer cannot be retrieved..." i PrepareAccessTokenPrincipal.
    /// </param>
    Task<(string AccessToken, string RefreshToken)> IssueTokenPairAsync(ApplicationUser user, Uri baseUri);
}